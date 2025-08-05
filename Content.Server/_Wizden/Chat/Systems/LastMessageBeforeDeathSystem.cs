using System.Text;
using Content.Server.Administration.Systems;
using Content.Server.Discord;
using Content.Shared.Mobs.Systems;
using Content.Server.GameTicking;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Content.Server._Wizden.Discord.Managers;
using Robust.Shared.Configuration;
using Content.Shared._Impstation.CCVar;
using Robust.Shared.Enums;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Server.Roles.Jobs;

namespace Content.Server._Wizden.Chat.Systems;

sealed class LastMessageBeforeDeathSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly DiscordWebhook _discord = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedMindSystem _sharedMind = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly JobSystem _jobs = default!;

    private bool _lastMessageWebhookEnabled = false;

    private bool _endOfRound = false;

    #region WebhookCvars
    private int _maxICLengthCVar;
    private int _maxMessageSize;
    private int _maxMessagesPerBatch;
    private int _messageDelayMs;
    private int _rateLimitDelayMs;
    #endregion WebhookCvars

    private WebhookIdentifier? _webhookIdentifierLastMessage;

    private readonly LastMessageWebhookManager _webhookManager = new LastMessageWebhookManager();

    private Dictionary<NetUserId, PlayerData> _playerData = new Dictionary<NetUserId, PlayerData>();

    // Holds player data, player data then has character data within
    private class PlayerData
    {
        public Dictionary<MindComponent, CharacterData> Characters = new Dictionary<MindComponent, CharacterData>();
        public ICommonSession? PlayerSession;
    }

    // Character data holds actual last message, then is associated with the actual player
    private class CharacterData
    {
        public string? LastMessage { get; set; } // Store only the last message
        public EntityUid EntityUid { get; set; }
        public string? JobTitle { get; set; }
        public TimeSpan MessageTime { get; set; } // Store the round time of the last message
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EraseEvent>(OnErase);
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChange);

        Subs.CVar(_configManager, ImpCCVars.DiscordLastMessageBeforeDeathWebhook, value =>
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                _lastMessageWebhookEnabled = true;
                _discord.GetWebhook(value, data => _webhookIdentifierLastMessage = data.ToIdentifier());
            }
        }, true);

        _configManager.OnValueChanged(ImpCCVars.DiscordLastMessageSystemMaxICLength, obj => _maxICLengthCVar = obj, true);
        _configManager.OnValueChanged(ImpCCVars.DiscordLastMessageSystemMaxMessageLength, obj => _maxMessageSize = obj, true);
        _configManager.OnValueChanged(ImpCCVars.DiscordLastMessageSystemMaxMessageBatch, obj => _maxMessagesPerBatch = obj, true);
        _configManager.OnValueChanged(ImpCCVars.DiscordLastMessageSystemMessageDelay, obj => _messageDelayMs = obj, true);
        _configManager.OnValueChanged(ImpCCVars.DiscordLastMessageSystemMaxMessageBatchOverflowDelay, obj => _rateLimitDelayMs = obj, true);
    }

    /// <summary>
    ///     Adds a message to the character data for a given player session.
    /// </summary>
    /// <param name="source">The entity UID of the source.</param>
    /// <param name="playerSession">The player's current session.</param>
    /// <param name="message">The message to be added.</param>
    public void AddMessage(EntityUid source, ICommonSession playerSession, string message)
    {
        if (!_lastMessageWebhookEnabled)
        {
            return;
        }

        if (_endOfRound == true)
        {
            return;
        }

        if (!_playerData.ContainsKey(playerSession.UserId))
        {
            _playerData[playerSession.UserId] = new PlayerData();
            _playerData[playerSession.UserId].PlayerSession = playerSession;
        }
        var playerData = _playerData[playerSession.UserId];

        var mindContainerComponent = CompOrNull<MindContainerComponent>(source);
        var mind = _sharedMind.GetMind(source, mindContainerComponent);
        var jobName = _jobs.MindTryGetJobName(mind);
        if (mindContainerComponent != null && mindContainerComponent.Mind != null)
        {
            if (TryComp<MindComponent>(mind, out var mindComponent) == false)
            {
                return;
            }
            if (mindComponent != null && !playerData.Characters.ContainsKey(mindComponent))
            {
                playerData.Characters[mindComponent] = new CharacterData();
            }
            if (mindComponent != null)
            {
                var characterData = playerData.Characters[mindComponent];
                characterData.LastMessage = message;
                characterData.EntityUid = source;
                characterData.JobTitle = jobName;
                characterData.MessageTime = _gameTicker.RoundDuration();
            }
        }
    }

    /// <summary>
    ///     Processes messages at the end of a round and sends them via webhook.
    /// </summary>
    public void OnRoundEnd()
    {
        if (!_lastMessageWebhookEnabled)
            return;

        _webhookManager.Initialize();

        var allMessages = new List<string>();

        foreach (var player in _playerData)
        {
            var singlePlayerData = player.Value;
            if (singlePlayerData.PlayerSession != null && singlePlayerData.PlayerSession.Status != SessionStatus.Disconnected)
            {
                foreach (var character in singlePlayerData.Characters)
                {
                    var characterData = character.Value;
                    // I am sure if there is a better way to go about checking if an EntityUID is no longer linked to an active entity...
                    // I don't know how tho...
                    if ((_mobStateSystem.IsDead(characterData.EntityUid) || !EntityManager.TryGetComponent<MetaDataComponent>(characterData.EntityUid, out var metadata)) && character.Key.CharacterName != null) // Check if an entity is dead or doesn't exist
                    {
                        var message = FormatMessage(characterData, character.Key.CharacterName);
                        allMessages.Add(message);
                    }
                    else
                    {
                        continue;
                    }
                }
            }
        }

        SendMessagesInBatches(allMessages);

        // Clear all stored data upon round restart
        _playerData.Clear();
    }

    /// <summary>
    ///     Formats a message for the "last message before death" system.
    /// </summary>
    /// <param name="characterData">The data of the character whose message is being formatted.</param>
    /// <param name="characterName">The name of the character whose message is being formatted.</param>
    /// <returns>A formatted message string.</returns>
    private string FormatMessage(CharacterData characterData, string characterName)
    {
        var message = characterData.LastMessage;
        if (message != null)
        {
            if (message.Length > _maxICLengthCVar)
            {
                message = message[.._maxICLengthCVar] + "-";
            }
            var messageTime = characterData.MessageTime;
            var jobTitle = $"{characterData.JobTitle}";
            var truncatedTime = $"{messageTime.Hours:D2}:{messageTime.Minutes:D2}:{messageTime.Seconds:D2}";

            return Loc.GetString("lastmessagewebhook-time-of-death", ("truncatedTime", truncatedTime), ("characterName", characterName), ("jobTitle", jobTitle), ("message", message));
        }
        else
        {
            return "";
        }
    }

    /// <summary>
    ///     Sends messages in batches via webhook.
    /// </summary>
    /// <param name="messages">The list of messages to be sent.</param>
    private void SendMessagesInBatches(List<string> messages)
    {
        var concatenatedMessages = new StringBuilder();
        var messagesToSend = new List<string>();

        foreach (var message in messages)
        {
            if (concatenatedMessages.Length + message.Length + 1 > _maxMessageSize)
            {
                messagesToSend.Add(concatenatedMessages.ToString());
                concatenatedMessages.Clear();
            }
            concatenatedMessages.AppendLine(message);
        }

        if (concatenatedMessages.Length > 0)
            messagesToSend.Add(concatenatedMessages.ToString());

        _webhookManager.SendMessagesAsync(_webhookIdentifierLastMessage, messagesToSend, _maxMessageSize, _maxMessagesPerBatch, _messageDelayMs, _rateLimitDelayMs);
    }

    private void OnErase(ref EraseEvent args)
    {
        _playerData.Remove(args.PlayerNetUserId);
    }

    private void OnRunLevelChange(GameRunLevelChangedEvent args)
    {
        if (args.New == GameRunLevel.PostRound)
        {
            _endOfRound = true;
        }
        if (args.New == GameRunLevel.InRound)
        {
            _endOfRound = false;
        }
    }
}
