using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared._Impstation.CCVar;

// ReSharper disable once InconsistentNaming
[CVarDefs]
public sealed class ImpCCVars : CVars
{
    /// <summary>
    /// Toggles the proximity warping effect on the singularity.
    /// This option is for people who generally do not mind motion, but find
    /// the singularity warping especially egregious.
    /// </summary>
    public static readonly CVarDef<bool> DisableSinguloWarping =
        CVarDef.Create("accessibility.disable_singulo_warping", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Should the player automatically get up after being knocked down
    /// </summary>
    public static readonly CVarDef<bool> AutoGetUp =
        CVarDef.Create("white.auto_get_up", true, CVar.CLIENT | CVar.ARCHIVE | CVar.REPLICATED); // WD EDIT

    /// <summary>
    /// The number of shared moods to give thaven by default.
    /// </summary>
    public static readonly CVarDef<uint> ThavenSharedMoodCount =
        CVarDef.Create<uint>("thaven.shared_mood_count", 1, CVar.SERVERONLY);

    /// <summary>
    /// URL of the Discord webhook which will relay last messages before death.
    /// </summary>
    public static readonly CVarDef<string> DiscordLastMessageBeforeDeathWebhook =
        CVarDef.Create("discord.last_message_before_death_webhook", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    /// A maximum length before an IC message is cut off in LastMessageBeforeDeathSystem during formatting.
    /// Can't be less than 1.
    /// Do not set this value above 2000, as that is the limit for discord webhook messages
    /// </summary>
    public static readonly CVarDef<int> DiscordLastMessageSystemMaxICLength =
        CVarDef.Create("discord.last_message_system_max_ic_length", 2000, CVar.SERVERONLY);

    /// <summary>
    /// A maximum length of a discord message that a webhook sends.
    /// Can't be more than 2000 and can't be less than 1.
    /// </summary>
    public static readonly CVarDef<int> DiscordLastMessageSystemMaxMessageLength =
        CVarDef.Create("discord.last_message_system_max_message_length", 2000, CVar.SERVERONLY);

    /// <summary>
    /// A maximum amount of a discord messages that a webhook sends in one batch.
    /// </summary>
    public static readonly CVarDef<int> DiscordLastMessageSystemMaxMessageBatch =
        CVarDef.Create("discord.last_message_system_max_message_batch", 15, CVar.SERVERONLY);

    /// <summary>
    /// Delay in milliseconds between each message the discord webhook sends.
    /// </summary>
    public static readonly CVarDef<int> DiscordLastMessageSystemMessageDelay =
        CVarDef.Create("discord.last_message_system_message_delay", 2000, CVar.SERVERONLY);

    /// <summary>
    /// If a maximum amount of messages per batch has been reached, we wait this amount of time (in milliseconds) to send what's left.
    /// </summary>
    public static readonly CVarDef<int> DiscordLastMessageSystemMaxMessageBatchOverflowDelay =
        CVarDef.Create("discord.last_message_system_max_message_batch_overflow_delay", 60000, CVar.SERVERONLY);
}
