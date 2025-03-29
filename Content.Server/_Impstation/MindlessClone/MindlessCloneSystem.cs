using Content.Server.Humanoid;
using Content.Server.Traits;
using Content.Server.Preferences.Managers;
using Content.Server.Mind;
using Content.Server.Chat.Systems;
using Content.Server.Body.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Traits;
using Content.Shared.Dataset;
using Content.Shared._Impstation.MindlessClone;
using Content.Shared.Tag;
using Content.Shared.Preferences;
using Content.Shared.Whitelist;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Speech.Components;
using Content.Shared.Speech;
using Content.Server.DoAfter;
using Content.Shared.DoAfter;
using Robust.Shared.Random;
using Content.Shared.Chat.TypingIndicator;
using Content.Shared.Inventory;
using Content.Shared.Pointing;
using System.Linq;
using Content.Server.Speech.Components;
using Content.Server.Kitchen.Components;
using Content.Server.NPC.HTN;
using Content.Shared.NPC.Components;
using Content.Shared.Examine;
using Content.Shared.Mind.Components;

namespace Content.Server._Impstation.MindlessClone;

public sealed class MindlessCloneSystem : SharedMindlessCloneSystem
{
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindlessCloneComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MindlessCloneComponent, MindlessCloneSayDoAfterEvent>(OnDoAfterComplete);
        SubscribeLocalEvent<MindlessCloneComponent, ExaminedEvent>(OnExamined);
    }

    private void OnMapInit(Entity<MindlessCloneComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp(ent, out HumanoidAppearanceComponent? humanoid) || !string.IsNullOrEmpty(humanoid.Initial))
            return;

        var cloneCoords = _transformSystem.GetMapCoordinates(ent.Owner);
        if (!TryGetNearestHumanoid(cloneCoords, out var target)) // grab the appearance data of the nearest humanoid with a mind.
            return;

        // break down the grabbed Entity<HumanoidAppearanceComponent> into its constituent parts.
        (var targetUid, var targetAppearance) = target.Value;
        ent.Comp.IsCloneOf = targetUid;

        HumanoidCharacterProfile profile; // and now we create a HumanoidCharacterProfile out of the target's profile data.
        if (_mind.TryGetMind(targetUid, out _, out var mindComponent) && mindComponent.Session != null)
        {
            // get the character profile of the humanoid out of its mind.
            var targetProfile = (HumanoidCharacterProfile)_prefs.GetPreferences(mindComponent.Session.UserId).SelectedCharacter;
            // clone it onto a new profile,
            profile = new HumanoidCharacterProfile(targetProfile);
            // then set the clone's name to the name of the target.
            _metaData.SetEntityName(ent, targetProfile.Name);
        }

        else // this shouldn't happen - TryGetNearestHumanoid should only be grabbing humanoids with minds.
        {
            profile = HumanoidCharacterProfile.DefaultWithSpecies(targetAppearance.Species)
            .WithSex(targetAppearance.Sex)
            .WithGender(targetAppearance.Gender);
        }

        // match the speech and emote sounds of the target
        if (TryComp<VocalComponent>(targetUid, out var targetVocal) && TryComp<VocalComponent>(ent, out var cloneVocal)
        && TryComp<SpeechComponent>(targetUid, out var targetSpeech) && TryComp<SpeechComponent>(ent, out var cloneSpeech))
        {
            cloneVocal.Sounds = targetVocal.Sounds;
            cloneVocal.EmoteSounds = targetVocal.EmoteSounds;

            cloneSpeech.SpeechSounds = targetSpeech.SpeechSounds;
            cloneSpeech.AllowedEmotes = targetSpeech.AllowedEmotes;
            cloneSpeech.SpeechVerb = targetSpeech.SpeechVerb;
            cloneSpeech.SuffixSpeechVerbs = targetSpeech.SuffixSpeechVerbs;
            cloneSpeech.SpeechBubbleOffset = targetSpeech.SpeechBubbleOffset;

            if (TryComp<TypingIndicatorComponent>(targetUid, out var targetTyping)) // this doesn't work. i don't know why and it's annoying, so i quit
                CopyComp(targetUid, ent, targetTyping);
        }

        // and finally, we load the final profile onto the spawned clone.
        _humanoid.LoadProfile(ent, profile, humanoid);

        // now we need to run some code to ensure traits get applied - since this isn't a player spawning, we need to basically duplicate the code from TraitSystem here, but without the bit that gives items.
        // No, I will not try to fix TraitSystem for this PR.
        if (profile.TraitPreferences.Count > 0)
        {
            foreach (var traitId in profile.TraitPreferences)
            {
                if (!_prototypeManager.TryIndex<TraitPrototype>(traitId, out var traitPrototype))
                {
                    Log.Warning($"No trait found with ID {traitId}!");
                    return;
                }

                if (_whitelistSystem.IsWhitelistFail(traitPrototype.Whitelist, ent) ||
                    _whitelistSystem.IsBlacklistPass(traitPrototype.Blacklist, ent))
                    continue;

                // Add all components required by the prototype to the body or specified organ
                if (traitPrototype.Organ != null)
                {
                    foreach (var organ in _bodySystem.GetBodyOrgans(ent))
                    {
                        if (traitPrototype.Organ is { } organTag && _tagSystem.HasTag(organ.Id, organTag))
                        {
                            EntityManager.AddComponents(organ.Id, traitPrototype.Components);
                        }
                    }
                }
                else
                {
                    EntityManager.AddComponents(ent, traitPrototype.Components, false);
                }
            }
        }

        // do our mindswapping logic before trying to speak, because otherwise the player starts the doafter but doesn't finish it.
        if (ent.Comp.MindSwap)
        {
            if (!_mind.TryGetMind(ent, out var cloneMind, out _) & !_mind.TryGetMind(targetUid, out var targetMind, out _))
                return;

            ent.Comp.MindSwap = false; // we don't want an infinite loop.

            // now we copy the MindlessClone componentry over to our new host, and remove those components from the clone's original body.
            CopyComp(ent, targetUid, ent.Comp);
            RemCompDeferred(ent, ent.Comp);
            ent.Comp.SpeakOnSpawn = false; // prevents the clone body from speaking - shouldn't prevent the original body from speaking, as it now has MindSwap set to false.

            var cloneActiveListener = EnsureComp<ActiveListenerComponent>(ent);
            var targetHasActiveListener = TryComp<ActiveListenerComponent>(targetUid, out var targetActiveListener);
            CopyComp(ent, targetUid, cloneActiveListener);
            if (!targetHasActiveListener)
                RemCompDeferred(ent, cloneActiveListener);
            else if (targetActiveListener != null)
                CopyComp(targetUid, ent, targetActiveListener!);

            var cloneParrotSpeech = EnsureComp<ParrotSpeechComponent>(ent);
            var targetHasParrotSpeech = TryComp<ParrotSpeechComponent>(targetUid, out var targetParrotSpeech);
            CopyComp(ent, targetUid, cloneParrotSpeech);
            if (!targetHasParrotSpeech)
                RemCompDeferred(ent, cloneParrotSpeech);
            else if (targetParrotSpeech != null)
                CopyComp(targetUid, ent, targetParrotSpeech);

            var htn = EnsureComp<HTNComponent>(ent); // players shouldn't have HTN, so I don't have to worry about this.
            CopyComp(ent, targetUid, htn);
            RemCompDeferred(ent, htn);

            var cloneNpcFactionMember = EnsureComp<NpcFactionMemberComponent>(ent);
            var targetNpcFactionMember = EnsureComp<NpcFactionMemberComponent>(targetUid);
            CopyComp(ent, targetUid, cloneNpcFactionMember);
            CopyComp(targetUid, ent, targetNpcFactionMember);

            var cloneMindContainer = EnsureComp<MindContainerComponent>(ent);
            var targetMindContainer = EnsureComp<MindContainerComponent>(ent);
            CopyComp(ent, targetUid, cloneMindContainer);
            CopyComp(targetUid, ent, targetMindContainer);

            _mind.TransferTo(cloneMind, targetUid);
            _mind.TransferTo(targetMind, ent);
        }

        if (ent.Comp.SpeakOnSpawn)
        {
            // start a DoAfter to delay our initial message by a bit. 
            _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, ent, _random.NextFloat(1f, 5f),
                new MindlessCloneSayDoAfterEvent(), ent, ent)
            {
                BlockDuplicate = true,
                BreakOnDamage = false,
                BreakOnMove = false,
                Hidden = true
            });

            // enable the typing indicator for the duration of the DoAfter.
            _appearance.SetData(ent.Owner, TypingIndicatorVisuals.IsTyping, true);
        }
    }

    public bool TryGetNearestHumanoid(MapCoordinates coordinates, [NotNullWhen(true)] out Entity<HumanoidAppearanceComponent>? target)
    {
        target = null;
        var minDistance = float.PositiveInfinity;

        var enumerator = EntityQueryEnumerator<HumanoidAppearanceComponent, TransformComponent>();
        while (enumerator.MoveNext(out var uid, out var targetComp, out var xform))
        {
            if (coordinates.MapId != xform.MapID)
                continue;
            if (!_mind.TryGetMind(uid, out _, out _))
                continue;

            var coords = _transformSystem.GetWorldPosition(xform);
            var distanceSquared = (coordinates.Position - coords).LengthSquared();
            if (!float.IsInfinity(minDistance) && distanceSquared >= minDistance)
                continue;

            minDistance = distanceSquared;
            target = (uid, targetComp);
        }

        return target != null;
    }

    private void OnDoAfterComplete(Entity<MindlessCloneComponent> ent, ref MindlessCloneSayDoAfterEvent args)
    {
        var choices = _prototypeManager.Index(ent.Comp.PhrasesToPick).Values;
        _chat.TrySendInGameICMessage(ent,
                    _random.Pick(choices),
                    InGameICChatType.Speak,
                    hideChat: false,
                    hideLog: true,
                    checkRadioPrefix: false);
        // ten percent chance to hit 2 after
        if (_random.Prob(0.1f))
            _chat.TrySendInGameICMessage(ent,
                    "screams!",
                    InGameICChatType.Emote,
                    hideChat: false,
                    hideLog: true,
                    checkRadioPrefix: false);
        // disable the typing indicator, as "typing" has now finished.
        _appearance.SetData(ent.Owner, TypingIndicatorVisuals.IsTyping, false);
    }

    private void OnExamined(Entity<MindlessCloneComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup($"[color=mediumpurple]{Loc.GetString("comp-mind-examined-mindlessclone", ("ent", ent.Owner))}[/color]");
    }
}
