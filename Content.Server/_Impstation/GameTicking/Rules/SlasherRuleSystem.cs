using Content.Server._Impstation.GameTicking.Rules.Components;
using Content.Server._Impstation.Slasher;
using Content.Server._Impstation.Slasher.Components;
using Content.Server.Antag;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.RoundEnd;
using Content.Shared.Dataset;
using Content.Shared._Impstation.Slasher.Components;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Random.Helpers;
using Content.Shared.Roles;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Impstation.GameTicking.Rules;

/// <summary>
/// Manages Slasher rule lifecycle: round state initialisation, antag spawning, and end-condition checking.
/// Effigy-specific logic lives in <see cref="SlasherEffigySystem"/>.
/// </summary>
public sealed class SlasherRuleSystem : GameRuleSystem<SlasherRuleComponent>
{
    private static readonly ProtoId<StartingGearPrototype> SlasherGearPrototypeId = "SlasherGear";
    private static readonly ProtoId<LocalizedDatasetPrototype> SlasherNameDatasetId = "NamesSlasher";

    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly SlasherDeathTeleportSystem _deathTeleport = default!;
    [Dependency] private readonly SlasherEffigySystem _effigy = default!;

    // Full set of starter-kit prototype IDs, keyed by prototype ID → equipment slot name.
    // Slot is null for inhand/storage items that have no suppressible slot.
    private HashSet<string> _starterKitPrototypeIds = new();
    private Dictionary<string, string> _starterKitProtoToSlot = new();
    private EntityQuery<ContainerManagerComponent> _containerQuery;
    private bool _initialized;

    /// <summary>
    /// Subscribes local events and prepares dependencies for this system.
    /// </summary>
    public override void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;
        base.Initialize();
        _containerQuery = GetEntityQuery<ContainerManagerComponent>();
        (_starterKitPrototypeIds, _starterKitProtoToSlot) = BuildStarterKitPrototypeSet();

        SubscribeLocalEvent<SlasherRuleComponent, AfterAntagEntitySelectedEvent>(OnAfterAntagSelected);
        // Evaluate evac completion after SlasherDeathTeleportSystem handles failed-state deaths.
        SubscribeLocalEvent<SlasherRoleComponent, SlasherFailedDeathProcessedEvent>(OnSlasherFailedDeathProcessed);
    }

    /// <summary>
    /// Initializes per-round Slasher rule counters and clears active-effigy state.
    /// </summary>
    /// <param name="uid">Rule entity UID.</param>
    /// <param name="comp">Rule state component.</param>
    /// <param name="gameRule">Base game-rule component.</param>
    /// <param name="args">Rule start event data.</param>
    protected override void Started(EntityUid uid, SlasherRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        comp.TargetInsertions = GetScaledTargetInsertions(comp);
        comp.FragmentInsertions = 0;
        comp.EvacTriggered = false;
        comp.EffigyDestroyed = false;
        comp.EffigyPlacedEver = false;
        comp.FinalPhaseTriggered = false;
        comp.VictoryTriggered = false;
        comp.RoundEndOutcome = SlasherRoundEndOutcome.Unset;
        comp.ActiveEffigy = null;
        _effigy.SyncEffigyPinpointers(null);
    }

    /// <summary>
    /// Appends Slasher round-end outcome text and identified Slasher player entries.
    /// </summary>
    /// <param name="uid">Rule entity UID.</param>
    /// <param name="component">Rule state component.</param>
    /// <param name="gameRule">Base game-rule component.</param>
    /// <param name="args">Round-end text aggregation event data.</param>
    protected override void AppendRoundEndText(EntityUid uid,
        SlasherRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        var antags = _antag.GetAntagIdentifiers(uid);
        if (antags.Count == 0)
            return;

        var plural = antags.Count > 1;
        var outcome = ResolveRoundEndOutcome(component);
        args.AddLine(Loc.GetString(GetOutcomeLocaleKey(outcome, plural)));
        args.AddLine(Loc.GetString("slasher-round-end-list-start"));

        foreach (var (_, sessionData, name) in antags)
        {
            args.AddLine(Loc.GetString("slasher-round-end-list-entry",
                ("name", name),
                ("username", sessionData.UserName)));
        }
    }

    /// <summary>
    /// Finalizes selected slasher setup: tags starter-kit ownership, applies a random name, and syncs support systems.
    /// </summary>
    /// <param name="ent">Active slasher rule entity and component.</param>
    /// <param name="args">Selected antag event payload.</param>
    private void OnAfterAntagSelected(Entity<SlasherRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        EntityUid? mindId = null;
        if (_mind.TryGetMind(args.EntityUid, out var ownerMindId, out _))
        {
            mindId = ownerMindId;
            TagStarterKitOwnership(args.EntityUid, ownerMindId);
        }

        AssignRandomSlasherName(args.EntityUid, mindId);

        // Some characters can spawn with pacifist; Slasher must always be able to fight.
        RemComp<PacifiedComponent>(args.EntityUid);

        _eye.RefreshVisibilityMask(args.EntityUid);
        _deathTeleport.TryTeleportToSpawnShuttle(args.EntityUid);
        _effigy.SyncEffigyPinpointers(ent.Comp.ActiveEffigy);
    }

    /// <summary>
    /// Assigns a random slasher name to both entity metadata and the player's mind character name.
    /// </summary>
    /// <param name="slasher">Slasher entity UID.</param>
    /// <param name="mindId">Optional owner mind UID when already resolved.</param>
    private void AssignRandomSlasherName(EntityUid slasher, EntityUid? mindId)
    {
        if (!_proto.TryIndex(SlasherNameDatasetId, out var dataset))
            return;

        var randomName = _random.Pick(dataset);
        _metaData.SetEntityName(slasher, randomName);

        if (mindId == null || !TryComp<MindComponent>(mindId, out var mind))
            return;

        mind.CharacterName = randomName;
        Dirty(mindId.Value, mind);
    }

    // Tags only canonical starter-kit items so two Slashers do not cross-reclaim same-prototype items.
    /// <summary>
    /// Traverses the slasher's containment graph and tags starter-kit entities with ownership metadata.
    /// </summary>
    /// <param name="slasher">Slasher entity UID whose containers are scanned.</param>
    /// <param name="ownerMind">Owner mind UID to stamp on tagged items.</param>
    private void TagStarterKitOwnership(EntityUid slasher, EntityUid ownerMind)
    {
        if (!_containerQuery.TryGetComponent(slasher, out var rootManager))
            return;

        var containerStack = new Stack<ContainerManagerComponent>();
        containerStack.Push(rootManager);

        do
        {
            foreach (var container in rootManager.Containers.Values)
            {
                foreach (var entity in container.ContainedEntities)
                {
                    TryTagStarterKitEntity(entity, ownerMind);

                    if (_containerQuery.TryGetComponent(entity, out var nestedManager))
                        containerStack.Push(nestedManager);
                }
            }
        } while (containerStack.TryPop(out rootManager));
    }

    /// <summary>
    /// Applies starter-kit ownership metadata when the entity prototype belongs to the canonical slasher kit set.
    /// </summary>
    /// <param name="entity">Candidate item entity UID.</param>
    /// <param name="ownerMind">Owner mind UID to record.</param>
    private void TryTagStarterKitEntity(EntityUid entity, EntityUid ownerMind)
    {
        if (MetaData(entity).EntityPrototype?.ID is not { } protoId
            || !_starterKitPrototypeIds.Contains(protoId))
        {
            return;
        }

        var ownership = EnsureComp<SlasherItemOwnershipComponent>(entity);
        ownership.OwnerMind = ownerMind;
        ownership.Source = SlasherOwnedItemSource.StarterKit;
        // Store slot so ReclaimOwnedItems can suppress it when a cosmetic variant covers the same slot.
        if (_starterKitProtoToSlot.TryGetValue(protoId, out var slot))
            ownership.Slot = slot;
    }

    /// <summary>
    /// Builds the canonical starter-kit prototype lookup sets from the configured slasher starting gear.
    /// </summary>
    /// <returns>
    /// Tuple containing:
    /// IDs: all starter-kit prototype IDs,
    /// protoToSlot: prototype ID to equipment-slot mapping for suppressible slot-based reclaim logic.
    /// </returns>
    private (HashSet<string> ids, Dictionary<string, string> protoToSlot) BuildStarterKitPrototypeSet()
    {
        var ids = new HashSet<string>();
        var protoToSlot = new Dictionary<string, string>();
        if (!_proto.TryIndex(SlasherGearPrototypeId, out var gear))
            return (ids, protoToSlot);

        // Equipment items are mapped by slot name so the reclaim system can suppress the right ones.
        foreach (var (slot, proto) in gear.Equipment)
        {
            ids.Add(proto);
            protoToSlot[(string)proto] = slot;
        }

        foreach (var proto in gear.Inhand)
            ids.Add(proto);

        foreach (var storageEntry in gear.Storage.Values)
        {
            foreach (var proto in storageEntry)
                ids.Add(proto);
        }

        return (ids, protoToSlot);
    }

    /// <summary>
    /// Tries to resolve the currently active slasher game-rule entity.
    /// </summary>
    /// <param name="rule">Resolved rule entity and component when present.</param>
    /// <returns>True when an active slasher rule exists; otherwise false.</returns>
    public bool TryGetActiveRule(out Entity<SlasherRuleComponent> rule)
    {
        var query = QueryActiveRules();
        if (query.MoveNext(out var uid, out _, out var comp, out _))
        {
            rule = (uid, comp);
            return true;
        }

        rule = default;
        return false;
    }

    // Triggers evacuation once all failed-state Slashers are dead.
    /// <summary>
    /// Requests round end once all slashers are dead after effigy-failure processing (unless victory path already took over).
    /// </summary>
    /// <param name="ent">Slasher role entity for the processed death.</param>
    /// <param name="args">Post-failure-death event payload.</param>
    private void OnSlasherFailedDeathProcessed(Entity<SlasherRoleComponent> ent, ref SlasherFailedDeathProcessedEvent args)
    {
        if (!TryGetActiveRule(out var rule))
            return;

        // Victory path manages its own shuttle; don't start a second one.
        if (rule.Comp.VictoryTriggered)
            return;

        if (!rule.Comp.EffigyDestroyed || rule.Comp.EvacTriggered)
            return;

        if (AnyAliveSlashers())
            return;

        rule.Comp.RoundEndOutcome = SlasherRoundEndOutcome.CrewMajor;
        rule.Comp.EvacTriggered = true;
        _roundEnd.RequestRoundEnd(TimeSpan.FromMinutes(5), null, checkCooldown: false,
            text: "slasher-threat-passed-announcement",
            name: "slasher-threat-passed-sender",
            cantRecall: true);
    }

    /// <summary>
    /// Resolves the most accurate round-end outcome for Slasher EOR text.
    /// </summary>
    /// <param name="component">Rule state to evaluate.</param>
    /// <returns>Resolved Slasher round-end outcome.</returns>
    private SlasherRoundEndOutcome ResolveRoundEndOutcome(SlasherRuleComponent component)
    {
        if (component.RoundEndOutcome != SlasherRoundEndOutcome.Unset)
            return component.RoundEndOutcome;

        if (component.VictoryTriggered)
            return SlasherRoundEndOutcome.SlasherMajor;

        return AnyAliveSlashers()
            ? SlasherRoundEndOutcome.CrewMinor
            : SlasherRoundEndOutcome.CrewMajor;
    }

    /// <summary>
    /// Returns the locale key for the resolved Slasher round-end outcome.
    /// </summary>
    /// <param name="outcome">Resolved outcome category.</param>
    /// <param name="plural">True when multiple slashers participated.</param>
    /// <returns>Locale key used for the top EOR result line.</returns>
    private static string GetOutcomeLocaleKey(SlasherRoundEndOutcome outcome, bool plural)
    {
        return outcome switch
        {
            SlasherRoundEndOutcome.SlasherMajor when plural => "slasher-round-end-slasher-major-plural",
            SlasherRoundEndOutcome.SlasherMajor => "slasher-round-end-slasher-major",
            SlasherRoundEndOutcome.CrewMinor when plural => "slasher-round-end-crew-minor-plural",
            SlasherRoundEndOutcome.CrewMinor => "slasher-round-end-crew-minor",
            SlasherRoundEndOutcome.CrewMajor when plural => "slasher-round-end-crew-major-plural",
            _ => "slasher-round-end-crew-major",
        };
    }

    /// <summary>
    /// Checks whether any Slasher-role entity is currently alive.
    /// </summary>
    /// <returns>True when at least one Slasher is alive.</returns>
    private bool AnyAliveSlashers()
    {
        var slashers = EntityQueryEnumerator<SlasherRoleComponent, MobStateComponent>();
        while (slashers.MoveNext(out _, out _, out var mobState))
        {
            if (mobState.CurrentState == MobState.Alive)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Scales required effigy insertions by connected player count between configured min/max bounds.
    /// </summary>
    /// <param name="comp">Rule state/configuration component.</param>
    /// <returns>Rounded insertion target clamped to configured bounds.</returns>
    private int GetScaledTargetInsertions(SlasherRuleComponent comp)
    {
        var players = 0;
        foreach (var session in _player.Sessions)
        {
            if (session.Status is SessionStatus.Disconnected or SessionStatus.Zombie)
                continue;

            players++;
        }

        if (comp.MaxPlayersForGoal <= comp.MinPlayersForGoal)
            return comp.MaxInsertions;

        var clampedPlayers = Math.Clamp(players, comp.MinPlayersForGoal, comp.MaxPlayersForGoal);
        var progress = (float)(clampedPlayers - comp.MinPlayersForGoal) /
                       (comp.MaxPlayersForGoal - comp.MinPlayersForGoal);

        var scaled = comp.MinInsertions + (comp.MaxInsertions - comp.MinInsertions) * progress;
        return Math.Clamp((int)MathF.Round(scaled), comp.MinInsertions, comp.MaxInsertions);
    }
}
