using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Server.DoAfter;
using Content.Server.Humanoid;
using Content.Server.Mind;
using Content.Server.Pointing.Components;
using Content.Server.Preferences.Managers;
using Content.Shared._Impstation.MindlessClone;
using Content.Shared.Chat.TypingIndicator;
using Content.Shared.Cloning;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Eye;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.Popups;
using Content.Shared.Preferences;
using Content.Shared.Speech.Muting;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Traits;
using Content.Shared.Whitelist;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Replays;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
// yes, all of these are really necessary. Christ almighty.

namespace Content.Server._Impstation.MindlessClone;

public sealed class MindlessCloneSystem : SharedMindlessCloneSystem
{
    // interfaces and managers
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IReplayRecordingManager _replay = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ISerializationManager _serManager = default!;
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;

    // everything else
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly RotateToFaceSystem _rotateToFaceSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly VisibilitySystem _visibilitySystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindlessCloneComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MindlessCloneComponent, MindlessCloneDelayDoAfterEvent>(OnDelayComplete);
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

        // clone the appearance and components of the original
        TryCloneNoOverwrite(targetUid, ent, ent.Comp.SettingsId);

        // do our mindswapping logic before trying to speak, because otherwise the player starts the doafter but doesn't finish it.
        if (ent.Comp.MindSwap)
        {
            if (!_mind.TryGetMind(ent, out var cloneMind, out _) & !_mind.TryGetMind(ent.Comp.IsCloneOf, out var targetMind, out _))
                return;

            ent.Comp.MindSwap = false; // we don't want an infinite loop.
            ent.Comp.OriginalBody = ent.Owner;

            // now we copy the MindlessClone component over to our new host.
            if (HasComp<MindlessCloneComponent>(targetUid)) // it damn well shouldn't.
                RemComp<MindlessCloneComponent>(targetUid);
            CopyComp(ent, targetUid, ent.Comp);
            RemCompDeferred(ent, ent.Comp);

            ent.Comp.SpeakOnSpawn = false; // prevent the clone body from speaking before its MindlessCloneComponent gets a chance to be removed.

            // ... and swap all our vital components over to the new body.
            foreach (var componentName in ent.Comp.ComponentsToSwap)
            {
                if (!_componentFactory.TryGetRegistration(componentName, out var componentRegistration))
                {
                    Log.Error($"Tried to use invalid component registration for MindlessClone mind-swapping: {componentName}");
                    continue;
                }

                // if either or both sides have it, copy it over to the other side, and then remove it from yourself.
                if (_entityManager.TryGetComponent(targetUid, componentRegistration.Type, out var sourceComp)
                | _entityManager.TryGetComponent(ent, componentRegistration.Type, out var cloneComp))
                {
                    if (sourceComp != null && cloneComp != null) // if both sides have a component, we need to be clever about it.
                    {
                        // create two new components of this type
                        var sourceCopy = _componentFactory.GetComponent(componentRegistration);
                        var cloneCopy = _componentFactory.GetComponent(componentRegistration);

                        // copy the settings over to those
                        _serManager.CopyTo(sourceComp, ref sourceCopy, notNullableOverride: true);
                        _serManager.CopyTo(cloneComp, ref cloneCopy, notNullableOverride: true);

                        // remove the originals
                        RemComp(ent, componentRegistration.Type);
                        RemComp(targetUid, componentRegistration.Type);

                        // and copy the new components onto their respective ents
                        CopyComp(ent, targetUid, cloneCopy);
                        CopyComp(targetUid, ent, sourceCopy);
                    }
                    else if (sourceComp != null)
                    {
                        if (HasComp(ent, componentRegistration.Type))
                            RemComp(ent, componentRegistration.Type);
                        CopyComp(targetUid, ent, sourceComp);
                        RemComp(targetUid, componentRegistration.Type);
                    }
                    else if (cloneComp != null)
                    {
                        if (HasComp(targetUid, componentRegistration.Type))
                            RemComp(targetUid, componentRegistration.Type);
                        CopyComp(ent, targetUid, cloneComp);
                        RemComp(ent, componentRegistration.Type);
                    }
                }
            }

            // swap those minds around right quick
            _mind.TransferTo(cloneMind, targetUid); // technically we won't ever need to do this, it's just here for posterity.
            _mind.TransferTo(targetMind, ent); // this is the important one. 

            // and then hobble the target for a bit
            _statusEffect.TryAddStatusEffect<MutedComponent>(ent, "Muted", ent.Comp.MindSwapStunTime, true);
        }

        var isMindSwapped = ent.Owner == ent.Comp.IsCloneOf; // determine if we're operating on a mindswapped clone in the target body
        TimeSpan stunTime;

        if (isMindSwapped)
        {
            stunTime = TimeSpan.FromSeconds(0.1); // skip stunning the mindswappee - the delay doafter is just there for the stack
        }
        else
        {
            stunTime = ent.Comp.MindSwapStunTime;
            _stun.TryParalyze(ent, stunTime, true);
        }

        // delay starting the typing DoAfter until the clone (or the mindswapped original) is done being stunned.
        // we do this on mindswapped clones too because otherwise their TypingIndicators don't show up properly.
        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, ent, stunTime + TimeSpan.FromSeconds(0.5), // need to add a little, otherwise the clone is 
            new MindlessCloneDelayDoAfterEvent(), ent, ent)
        {
            BlockDuplicate = true,
            BreakOnDamage = false,
            BreakOnMove = false,
            RequireCanInteract = false,
            HiddenFromUser = true
        });
    }

    /// <summary>
    /// Gets the nearest entity on the same map with HumanoidAppearanceComponent and a mind.
    /// </summary>
    /// <param name="coordinates"></param>
    /// <param name="target"></param>
    /// <returns></returns>
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

    /// <summary>
    /// A near copy of CloningSystem.TryClone, except without the bit where it spawns a whole ass new entity (which was annoying)
    /// and with a new bit about copying over traits.
    /// </summary>
    /// <param name="original"></param>
    /// <param name="clone"></param>
    /// <param name="settingsId"></param>
    /// <returns></returns>
    public bool TryCloneNoOverwrite(EntityUid original, EntityUid clone, ProtoId<CloningSettingsPrototype> settingsId)
    {
        HumanoidCharacterProfile profile;
        if (!TryComp<HumanoidAppearanceComponent>(original, out var originalAppearance) || originalAppearance == null)
            return false;

        if (_mind.TryGetMind(original, out _, out var mindComponent) && mindComponent.Session != null)
        {
            // get the character profile of the humanoid out of its mind.
            var targetProfile = (HumanoidCharacterProfile)_prefs.GetPreferences(mindComponent.Session.UserId).SelectedCharacter;
            // clone it onto a new profile
            profile = new HumanoidCharacterProfile(targetProfile);
        }
        else // this shouldn't happen - TryGetNearestHumanoid should only be grabbing humanoids with minds.
        {
            profile = HumanoidCharacterProfile.DefaultWithSpecies(originalAppearance.Species)
            .WithSex(originalAppearance.Sex)
            .WithGender(originalAppearance.Gender);
        }

        if (!_prototypeManager.TryIndex(settingsId, out var settings))
            return false;

        if (!TryComp<HumanoidAppearanceComponent>(original, out var humanoid))
            return false;

        if (!_prototypeManager.TryIndex(humanoid.Species, out var speciesPrototype))
            return false;

        _humanoid.CloneAppearance(original, clone);

        var componentsToCopy = settings.Components;

        if (TryComp<StatusEffectsComponent>(original, out var statusComp))
            componentsToCopy.ExceptWith(statusComp.ActiveEffects.Values.Select(s => s.RelevantComponent).Where(s => s != null)!);

        if (TryComp<NpcFactionMemberComponent>(original, out var npcFactionComp))
            componentsToCopy.Remove("NpcFactionMember"); // we wanna make sure that we're not putting you and your evil clone on the same side.

        foreach (var componentName in componentsToCopy)
        {
            if (!_componentFactory.TryGetRegistration(componentName, out var componentRegistration))
            {
                Log.Error($"Tried to use invalid registration for MindlessClone cloning: {componentName}");
                continue;
            }

            if (_entityManager.TryGetComponent(original, componentRegistration.Type, out var sourceComp))
            {
                if (HasComp(clone, componentRegistration.Type))
                    RemComp(clone, componentRegistration.Type);
                CopyComp(original, clone, sourceComp);
            }
        }

        var originalName = Name(original);

        _metaData.SetEntityName(clone, originalName);

        // now we need to run some code to ensure traits get applied - since this isn't a player spawning, we need to basically duplicate the code from TraitSystem here, but without the bit that gives items.
        // No, I will not try to fix TraitSystem for this PR.
        if (profile.TraitPreferences.Count > 0)
        {
            foreach (var traitId in profile.TraitPreferences)
            {
                if (!_prototypeManager.TryIndex<TraitPrototype>(traitId, out var traitPrototype))
                {
                    Log.Warning($"No trait found with ID {traitId}!");
                    continue;
                }

                if (_whitelistSystem.IsWhitelistFail(traitPrototype.Whitelist, clone) ||
                    _whitelistSystem.IsBlacklistPass(traitPrototype.Blacklist, clone))
                    continue;

                // Add all components required by the prototype to the body or specified organ
                if (traitPrototype.Organ != null)
                {
                    foreach (var organ in _bodySystem.GetBodyOrgans(clone))
                    {
                        if (traitPrototype.Organ is { } organTag && _tagSystem.HasTag(organ.Id, organTag))
                        {
                            EntityManager.AddComponents(organ.Id, traitPrototype.Components);
                        }
                    }
                }
                else
                {
                    EntityManager.AddComponents(clone, traitPrototype.Components, false);
                }
            }
        }

        return true;
    }

    private void OnDelayComplete(Entity<MindlessCloneComponent> ent, ref MindlessCloneDelayDoAfterEvent args)
    {
        // doing this here for stack reasons. if i try to do it OnMapInit, BloodstreamComponent hasn't had a chance to make the solutions yet.
        // this makes for the slightly funny side-effect that clones will bleed SynthFlesh while stunned, but once the stun is up, they'll bleed whatever their target bleeds.
        // fun little bit of worldbuilding there, if you think about it. 
        if (TryComp<BloodstreamComponent>(ent.Comp.IsCloneOf, out var originalBloodComp) && originalBloodComp != null
        && TryComp<BloodstreamComponent>(ent, out var cloneBloodComp))
        {
            _bloodstream.ChangeBloodReagent(ent, originalBloodComp.BloodReagent, cloneBloodComp);
        }

        // if we're supposed to speak on spawn, try to speak on spawn. as long as you're not crit or dead
        if (ent.Comp.SpeakOnSpawn && !_mobState.IsIncapacitated(ent))
        {
            // enable the typing indicator for the duration of the DoAfter.
            _appearance.SetData(ent.Owner, TypingIndicatorVisuals.IsTyping, true);

            // start a DoAfter to delay our initial message by a bit. 
            _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, ent, _random.NextFloat(1f, 5f),
                new MindlessCloneSayDoAfterEvent(), ent, ent)
            {
                BlockDuplicate = true,
                BreakOnDamage = false,
                BreakOnMove = false,
                RequireCanInteract = false,
                HiddenFromUser = true
            });
        }
    }

    private void OnDoAfterComplete(Entity<MindlessCloneComponent> ent, ref MindlessCloneSayDoAfterEvent args)
    {
        var choices = _prototypeManager.Index(ent.Comp.PhrasesToPick).Values;
        if (ent.Owner == ent.Comp.IsCloneOf) // If we've mindswapped, behavior should be a little different.
        {
            _chat.TrySendInGameICMessage(ent,
                    Loc.GetString(_random.Pick(choices)),
                    InGameICChatType.Speak,
                    hideChat: false,
                    hideLog: true,
                    checkRadioPrefix: false);
            // higher chance, but not guaranteed, to point at the original body.
            if (_random.Prob(0.6f))
                TryFakePoint(ent, ent.Comp.OriginalBody);
        }
        else
        {
            _chat.TrySendInGameICMessage(ent,
                    Loc.GetString(_random.Pick(choices)),
                    InGameICChatType.Speak,
                    hideChat: false,
                    hideLog: true,
                    checkRadioPrefix: false);
            // twenty percent chance to hit 2 after
            if (_random.Prob(0.2f))
                _chat.TrySendInGameICMessage(ent,
                    "screams!",
                    InGameICChatType.Emote,
                    hideChat: false,
                    hideLog: true,
                    checkRadioPrefix: false);
            // twenty percent chance to point at the original
            if (_random.Prob(0.2f))
                TryFakePoint(ent, ent.Comp.IsCloneOf);
        }

        // disable the typing indicator, as "typing" has now finished.
        _appearance.SetData(ent.Owner, TypingIndicatorVisuals.IsTyping, false);
    }

    /// <summary>
    /// Custom examine text for when the clone is not crit or dead. 
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="args"></param>
    private void OnExamined(Entity<MindlessCloneComponent> ent, ref ExaminedEvent args)
    {
        if (_mobState.IsAlive(ent))
            args.PushMarkup($"[color=mediumpurple]{Loc.GetString("comp-mind-examined-mindlessclone", ("ent", ent.Owner))}[/color]");
    }

    /// <summary>
    /// This is largely a copy of PointingSystem.TryPoint. However, due to the way PointingSystem works, I can't just use the pointing events,
    /// on account of the clones not having a player session attached to them. Has no regard for range or blocking walls.
    /// </summary>
    /// <param name="pointer"></param>
    /// <param name="pointee"></param>
    private void TryFakePoint(EntityUid pointer, EntityUid pointee)
    {
        var coordsPointee = Transform(pointee).Coordinates;


        var mapCoordsPointee = _transformSystem.ToMapCoordinates(coordsPointee);
        _rotateToFaceSystem.TryFaceCoordinates(pointer, mapCoordsPointee.Position);

        var arrow = EntityManager.SpawnEntity("PointingArrow", coordsPointee);

        if (TryComp<PointingArrowComponent>(arrow, out var pointing))
        {
            pointing.StartPosition = _transformSystem.ToCoordinates((arrow, Transform(arrow)), _transformSystem.ToMapCoordinates(Transform(pointer).Coordinates)).Position;
            pointing.EndTime = _gameTiming.CurTime + TimeSpan.FromSeconds(4);

            Dirty(arrow, pointing);
        }

        var layer = (int) VisibilityFlags.Normal;
        if (TryComp(pointer, out VisibilityComponent? playerVisibility))
        {
            var arrowVisibility = EntityManager.EnsureComponent<VisibilityComponent>(arrow);
            layer = playerVisibility.Layer;
            _visibilitySystem.SetLayer((arrow, arrowVisibility), (ushort)layer);
        }

        // Get players that are in range and whose visibility layer matches the arrow's.
        bool ViewerPredicate(ICommonSession playerSession)
        {
            if (!_mind.TryGetMind(playerSession, out _, out var mind) ||
                mind.CurrentEntity is not { Valid: true } ent ||
                !TryComp(ent, out EyeComponent? eyeComp) ||
                (eyeComp.VisibilityMask & layer) == 0)
                return false;

            return _transformSystem.GetMapCoordinates(ent).InRange(_transformSystem.GetMapCoordinates(pointer), 15f);
        }

        var viewers = Filter.Empty()
            .AddWhere(session1 => ViewerPredicate(session1))
            .Recipients;

        var pointerName = Identity.Entity(pointer, EntityManager);
        var pointeeName = Identity.Entity(pointee, EntityManager);

        var viewerMessage = Loc.GetString("pointing-system-point-at-other-others", ("otherName", pointerName), ("other", pointeeName));
        var viewerPointedAtMessage = Loc.GetString("pointing-system-point-at-you-other", ("otherName", pointerName));

        SendMessage(pointer, viewers, pointee, "", viewerMessage, viewerPointedAtMessage);
    }

    /// <summary>
    /// Used in fake pointing for the popup messages.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="viewers"></param>
    /// <param name="pointed"></param>
    /// <param name="selfMessage"></param>
    /// <param name="viewerMessage"></param>
    /// <param name="viewerPointedAtMessage"></param>
    private void SendMessage(
        EntityUid source,
        IEnumerable<ICommonSession> viewers,
        EntityUid pointed,
        string selfMessage,
        string viewerMessage,
        string? viewerPointedAtMessage = null)
    {
        var netSource = GetNetEntity(source);

        foreach (var viewer in viewers)
        {
            if (viewer.AttachedEntity is not {Valid: true} viewerEntity)
            {
                continue;
            }

            var message = viewerEntity == source
                ? selfMessage
                : viewerEntity == pointed && viewerPointedAtMessage != null
                    ? viewerPointedAtMessage
                    : viewerMessage;

            // Someone pointing at YOU is slightly more important
            var popupType = viewerEntity == pointed ? PopupType.Medium : PopupType.Small;

            RaiseNetworkEvent(new PopupEntityEvent(message, popupType, netSource), viewerEntity);
        }

        _replay.RecordServerMessage(new PopupEntityEvent(viewerMessage, PopupType.Small, netSource));
    }
}
