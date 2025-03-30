using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.Speech.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Whitelist;
using Content.Server.DoAfter; //imp
using Content.Shared.DoAfter; //imp
using Content.Shared._NF.Speech; //imp
using Content.Shared.Chat.TypingIndicator; //imp
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Server.GameObjects; //imp

namespace Content.Server.Speech.EntitySystems;

public sealed class ParrotSpeechSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ParrotSpeechComponent, ListenEvent>(OnListen);
        SubscribeLocalEvent<ParrotSpeechComponent, ListenAttemptEvent>(CanListen);
        SubscribeLocalEvent<ParrotSpeechComponent, ParrotSpeechDoAfterEvent>(OnDoAfter);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ParrotSpeechComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.LearnedPhrases.Count == 0)
                // This parrot has not learned any phrases, so can't say anything interesting.
                continue;

            if (component.RequiresMind && // imp 
            !TryComp<MindContainerComponent>(uid, out var mind) | mind != null && !mind!.HasMind)
                continue; // end imp

            if (_timing.CurTime < component.NextUtterance)
                continue;

            if (component.NextUtterance != null) // imp - changed this whole deal
            {
                if (component.FakeTypingIndicator)
                {
                    component.NextMessage = _random.Pick(component.LearnedPhrases);
                    var doAfterLength = TimeSpan.FromSeconds(0.1 * component.NextMessage.Length);
                    _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, uid, doAfterLength,
                    new ParrotSpeechDoAfterEvent(), uid, uid)
                    {
                        BlockDuplicate = true,
                        BreakOnDamage = false,
                        BreakOnMove = false,
                        RequireCanInteract = false,
                        HiddenFromUser = true
                    });
                    _appearance.SetData(uid, TypingIndicatorVisuals.IsTyping, true);
                }
                else
                {
                    SendMessage(uid, component);
                }
            } // end imp

            component.NextUtterance = _timing.CurTime + TimeSpan.FromSeconds(_random.Next(component.MinimumWait, component.MaximumWait));
        }
    }

    private void SendMessage(EntityUid uid, ParrotSpeechComponent component) // imp. moved this out of Update() and to its own method to reduce repitition repitition.
    {
        _chat.TrySendInGameICMessage(
        uid,
            component.NextMessage ?? _random.Pick(component.LearnedPhrases),
            InGameICChatType.Speak,
            hideChat: component.HideMessagesInChat, // Don't spam the chat with randomly generated messages(... unless its funny (imp change))
            hideLog: true, // TODO: Don't spam admin logs either. If a parrot learns something inappropriate, admins can search for the player that said the inappropriate thing.
            checkRadioPrefix: false);
    }

    private void OnDoAfter(Entity<ParrotSpeechComponent> ent, ref ParrotSpeechDoAfterEvent args)
    {
        SendMessage(ent.Owner, ent.Comp);
        _appearance.SetData(ent.Owner, TypingIndicatorVisuals.IsTyping, false);
    }

    private void OnListen(EntityUid uid, ParrotSpeechComponent component, ref ListenEvent args)
    {
        if (_random.Prob(component.LearnChance))
        {
            // Very approximate word splitting. But that's okay: parrots aren't smart enough to
            // split words correctly.
            var words = args.Message.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            // Prefer longer phrases
            var phraseLength = 1 + (int) (Math.Sqrt(_random.NextDouble()) * component.MaximumPhraseLength);

            var startIndex = _random.Next(0, Math.Max(0, words.Length - phraseLength + 1));

            var phrase = string.Join(" ", words.Skip(startIndex).Take(phraseLength)).ToLower();

            while (component.LearnedPhrases.Count >= component.MaximumPhraseCount)
            {
                _random.PickAndTake(component.LearnedPhrases);
            }

            component.LearnedPhrases.Add(phrase);
        }
    }

    private void CanListen(EntityUid uid, ParrotSpeechComponent component, ref ListenAttemptEvent args)
    {
        if (_whitelistSystem.IsBlacklistPass(component.Blacklist, args.Source))
            args.Cancel();
    }
}
