using Content.Server.Body.Systems;
using Content.Server.EUI;
using Content.Server.Ghost;
using Content.Server._Impstation.GameTicking.Rules;
using Content.Server._Impstation.Slasher.Components;
using Content.Shared.Actions;
using Content.Shared._EE.Carrying;
using Content.Shared._Impstation.Decapoids;
using Content.Shared._Impstation.Slasher;
using Content.Shared._Impstation.Slasher.Components;
using Content.Shared.Body.Components;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using SharedSlasherSoulHarvestedComponent = Content.Shared._Impstation.Slasher.Components.SlasherSoulHarvestedComponent;

namespace Content.Server._Impstation.Slasher;

/// <summary>
/// Handles Slasher-specific meathook state after a victim is mounted on the spike.
/// This covers hook visuals, passive victim stabilization, and manual soul-fragment harvesting.
/// </summary>
public sealed class SlasherMeatHookSystem : EntitySystem
{
    private const string DistanceLocArg = "distance";

    /// <summary>
    /// Enumeration values for PlacementFailureReason.
    /// </summary>
    private enum PlacementFailureReason
    {
        None,
        NoGrid,
        EmptyOrSpace,
        Blocked,
    }

    /// <summary>
    /// Prototype ID for the Slasher meathook entity spawned by the placement action.
    /// </summary>
    private static readonly EntProtoId SlasherMeathookProto = "SlasherMeatHook";

    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;
    [Dependency] private readonly SlasherRuleSystem _rule = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedWieldableSystem _wield = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <summary>
    /// Subscribes meathook placement, victim lifecycle, interaction, and harvest handlers.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlasherPlaceMeathookEvent>(OnPlaceMeathook);
        SubscribeLocalEvent<SlasherRoleComponent, SlasherPlaceMeathookDoAfterEvent>(OnPlaceMeathookDoAfter);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<SlasherMeatHookComponent, EntInsertedIntoContainerMessage>(OnVictimHooked);
        SubscribeLocalEvent<SlasherMeatHookComponent, EntRemovedFromContainerMessage>(OnVictimUnhooked);
        SubscribeLocalEvent<SlasherMeatHookComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<SlasherMeatHookComponent, SlasherHarvestSoulDoAfterEvent>(OnHarvestDoAfter);
        SubscribeLocalEvent<SlasherSoulFragmentComponent, GotUnequippedHandEvent>(OnSoulFragmentLeftHands);
        SubscribeLocalEvent<SharedSlasherSoulHarvestedComponent, ExaminedEvent>(OnHarvestedExamined);
    }

    /// <summary>
    /// Tracks recent death times for humanoid bodies so hook eligibility can include fresh corpses.
    /// </summary>
    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (!HasComp<HumanoidAppearanceComponent>(args.Target))
            return;

        if (args.NewMobState == MobState.Dead)
        {
            var recentDeath = EnsureComp<SlasherRecentDeathComponent>(args.Target);
            recentDeath.TimeOfDeath = _timing.CurTime;
            Dirty(args.Target, recentDeath);
            return;
        }

        if (TryComp<SlasherRecentDeathComponent>(args.Target, out var revivedMarker) && revivedMarker.TimeOfDeath != null)
        {
            revivedMarker.TimeOfDeath = null;
            Dirty(args.Target, revivedMarker);
        }
    }

    /// <summary>
    /// Handles world-targeted meathook placement using the standard action and charge systems.
    /// </summary>
    /// <param name="args">World-target meathook action event data.</param>
    private void OnPlaceMeathook(SlasherPlaceMeathookEvent args)
    {
        if (args.Handled || !HasComp<SlasherRoleComponent>(args.Performer))
            return;

        if (!_rule.TryGetActiveRule(out var rule))
            return;

        var mapCoords = _transform.ToMapCoordinates(args.Target);
        if (!TryGetPlacementCoordinates(mapCoords, out var spawnCoords, out var failure))
        {
            PopupPlacementFailure(args.Performer, failure, "slasher-meathook-place-invalid");
            return;
        }

        var hookConfig = GetMeathookConfig();

        if (!Transform(args.Performer).Coordinates.TryDistance(EntityManager, spawnCoords, out var placementDistance)
            || placementDistance > hookConfig.PlacementRange)
        {
            _popup.PopupEntity(Loc.GetString("slasher-place-invalid-too-far", (DistanceLocArg, hookConfig.PlacementRange)),
                args.Performer,
                args.Performer,
                PopupType.MediumCaution);
            return;
        }

        var minEffigyDistance = hookConfig.MinimumEffigyDistance;

        if (rule.Comp.ActiveEffigy is { } effigy
            && Exists(effigy)
            && Transform(effigy).Coordinates.TryDistance(EntityManager, spawnCoords, out var distance)
            && distance < minEffigyDistance)
        {
            _popup.PopupEntity(Loc.GetString("slasher-meathook-place-too-close-effigy", (DistanceLocArg, minEffigyDistance)),
                args.Performer,
                args.Performer,
                PopupType.MediumCaution);
            return;
        }

        var removeOnUse = TryComp<LimitedChargesComponent>(args.Action, out var charges)
            && _charges.GetCurrentCharges((args.Action, charges)) == 1;

        var doAfterEvent = new SlasherPlaceMeathookDoAfterEvent(
            EntityManager.GetNetCoordinates(spawnCoords),
            GetNetEntity(args.Action.Owner),
            removeOnUse);

        var doAfterArgs = new DoAfterArgs(EntityManager,
            args.Performer,
            hookConfig.PlacementDoAfterDelay,
            doAfterEvent,
            args.Performer,
            used: args.Performer)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfterArgs))
            return;

        args.Handled = true;
    }

    /// <summary>
    /// Finalizes meathook placement after the channeling do-after completes.
    /// </summary>
    private void OnPlaceMeathookDoAfter(Entity<SlasherRoleComponent> ent, ref SlasherPlaceMeathookDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (!_rule.TryGetActiveRule(out var rule))
            return;

        var requestedCoords = EntityManager.GetCoordinates(args.TargetCoordinates);
        var mapCoords = _transform.ToMapCoordinates(requestedCoords);
        if (!TryGetPlacementCoordinates(mapCoords, out var spawnCoords, out var failure))
        {
            PopupPlacementFailure(ent.Owner, failure, "slasher-meathook-place-invalid");
            return;
        }

        var hookConfig = GetMeathookConfig();

        if (!Transform(ent).Coordinates.TryDistance(EntityManager, spawnCoords, out var placementDistance)
            || placementDistance > hookConfig.PlacementRange)
        {
            _popup.PopupEntity(Loc.GetString("slasher-place-invalid-too-far", (DistanceLocArg, hookConfig.PlacementRange)),
                ent,
                ent,
                PopupType.MediumCaution);
            return;
        }

        var minEffigyDistance = hookConfig.MinimumEffigyDistance;

        if (rule.Comp.ActiveEffigy is { } effigy
            && Exists(effigy)
            && Transform(effigy).Coordinates.TryDistance(EntityManager, spawnCoords, out var distance)
            && distance < minEffigyDistance)
        {
            _popup.PopupEntity(Loc.GetString("slasher-meathook-place-too-close-effigy", (DistanceLocArg, minEffigyDistance)),
                ent,
                ent,
                PopupType.MediumCaution);
            return;
        }

        Spawn(SlasherMeathookProto, spawnCoords);
        rule.Comp.MeathookCount++;

        if (args.RemoveActionOnSuccess)
        {
            var actionEntity = GetEntity(args.ActionEntity);
            if (Exists(actionEntity))
                _actions.RemoveAction(ent.Owner, actionEntity);
        }
    }

    /// <summary>
    /// Runs periodic meathook stabilization/healing for currently hooked victims.
    /// </summary>
    /// <param name="frameTime">Frame delta time in seconds.</param>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var healers = EntityQueryEnumerator<SlasherMeatHookHealingComponent>();

        while (healers.MoveNext(out var uid, out var healing))
        {
            if (healing.NextHealTime > now)
                continue;

            healing.NextHealTime = now + healing.HealInterval;

            if (!IsStillHookedVictim(uid, healing))
                continue;

            if (_mobState.IsDead(uid) || !TryComp(uid, out DamageableComponent? damageable))
                continue;

            ApplyDamageHealing(uid, healing, damageable);

            if (TryComp(uid, out BloodstreamComponent? bloodstream))
                _bloodstream.TryRegulateBloodLevel((uid, bloodstream), bloodstream.BloodReferenceSolution.Volume, healing.TargetBloodLevel);
        }
    }

    /// <summary>
    /// Initializes hook visuals and victim runtime healing state when a body is inserted into the hook container.
    /// </summary>
    /// <param name="ent">Hook entity and hook component data.</param>
    /// <param name="args">Container insertion event data.</param>
    private void OnVictimHooked(Entity<SlasherMeatHookComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (!TryComp<SlasherMeatHookSpikeComponent>(ent, out var spikeComp) || args.Container.ID != spikeComp.ContainerId)
            return;

        var status = SlasherMeatHookStatus.PendingHarvest;
        if (TryComp<SharedSlasherSoulHarvestedComponent>(args.Entity, out var harvested) && harvested.ExpiresAt > _timing.CurTime)
            status = SlasherMeatHookStatus.Harvested;

        _appearance.SetData(ent, SlasherMeatHookVisuals.Status, status);
        _pointLight.SetColor(ent, status == SlasherMeatHookStatus.PendingHarvest
            ? ent.Comp.PendingHarvestLightColor
            : ent.Comp.HarvestedLightColor);
        _pointLight.SetEnabled(ent, true);

        var healing = EnsureComp<SlasherMeatHookHealingComponent>(args.Entity);
        healing.Hook = ent.Owner;
        healing.HealInterval = ent.Comp.HealInterval;
        healing.NextHealTime = _timing.CurTime + ent.Comp.HealInterval;
        healing.HealPerTick = ent.Comp.HealPerTick;
        healing.TargetDamage = ent.Comp.TargetDamage;
        healing.TargetBloodLevel = ent.Comp.TargetBloodLevel;
    }

    /// <summary>
    /// Clears hook visuals and removes victim runtime healing state when body is removed from hook container.
    /// </summary>
    /// <param name="ent">Hook entity and hook component data.</param>
    /// <param name="args">Container removal event data.</param>
    private void OnVictimUnhooked(Entity<SlasherMeatHookComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (!TryComp<SlasherMeatHookSpikeComponent>(ent, out var spikeComp) || args.Container.ID != spikeComp.ContainerId)
            return;

        _appearance.SetData(ent, SlasherMeatHookVisuals.Status, SlasherMeatHookStatus.Empty);
        _pointLight.SetEnabled(ent, false);
        RemComp<SlasherMeatHookHealingComponent>(args.Entity);
    }

    /// <summary>
    /// Starts soul-harvest interaction when a slasher interacts with their hook.
    /// </summary>
    /// <param name="ent">Hook entity and hook component data.</param>
    /// <param name="args">Hand interaction event data.</param>
    private void OnInteractHand(Entity<SlasherMeatHookComponent> ent, ref InteractHandEvent args)
    {
        if (!TryComp<SlasherMeatHookSpikeComponent>(ent, out var spike) || !HasComp<SlasherRoleComponent>(args.User))
            return;

        args.Handled = true;

        var victim = spike.BodyContainer.ContainedEntity;
        if (!victim.HasValue)
        {
            _popup.PopupEntity(Loc.GetString("slasher-meathook-harvest-no-victim"), args.User, args.User, PopupType.Medium);
            return;
        }

        if (!CanHarvestSoul(victim.Value))
        {
            _popup.PopupEntity(Loc.GetString("slasher-meathook-harvest-invalid-target"), args.User, args.User, PopupType.MediumCaution);
            return;
        }

        if (!CanStartHarvest(args.User, out _))
        {
            _popup.PopupEntity(Loc.GetString("slasher-meathook-harvest-empty-hand"), args.User, args.User, PopupType.MediumCaution);
            return;
        }

        if (TryComp<SharedSlasherSoulHarvestedComponent>(victim.Value, out var harvested) && harvested.ExpiresAt > _timing.CurTime)
        {
            _popup.PopupEntity(Loc.GetString("slasher-meathook-harvest-blocked"), args.User, args.User, PopupType.MediumCaution);
            return;
        }

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.HarvestDelay,
            new SlasherHarvestSoulDoAfterEvent(),
            ent,
            target: victim,
            used: args.User)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            BreakOnHandChange = true,
            NeedHand = true,
        });
    }

    /// <summary>
    /// Completes soul harvest do-after validation and grants the soul fragment when successful.
    /// </summary>
    /// <param name="ent">Hook entity and hook component data.</param>
    /// <param name="args">Harvest do-after event data.</param>
    private void OnHarvestDoAfter(Entity<SlasherMeatHookComponent> ent, ref SlasherHarvestSoulDoAfterEvent args)
    {
        if (args.Cancelled)
        {
            if (!CanStartHarvest(args.User, out _))
                _popup.PopupEntity(Loc.GetString("slasher-meathook-harvest-empty-hand"), args.User, args.User, PopupType.MediumCaution);

            return;
        }

        if (args.Target == null || !TryComp<SlasherMeatHookSpikeComponent>(ent, out var spike))
            return;

        var victim = spike.BodyContainer.ContainedEntity;
        if (!victim.HasValue || victim.Value != args.Target.Value)
            return;

        if (!CanHarvestSoul(victim.Value))
        {
            _popup.PopupEntity(Loc.GetString("slasher-meathook-harvest-invalid-target"), args.User, args.User, PopupType.MediumCaution);
            return;
        }

        // Revalidate lockout in case another harvest completed during this do-after window.
        if (TryComp<SharedSlasherSoulHarvestedComponent>(victim.Value, out var harvested) && harvested.ExpiresAt > _timing.CurTime)
        {
            _popup.PopupEntity(Loc.GetString("slasher-meathook-harvest-blocked"), args.User, args.User, PopupType.MediumCaution);
            return;
        }

        if (!CanStartHarvest(args.User, out var hands))
        {
            _popup.PopupEntity(Loc.GetString("slasher-meathook-harvest-empty-hand"), args.User, args.User, PopupType.MediumCaution);
            return;
        }

        var marker = EnsureComp<SharedSlasherSoulHarvestedComponent>(victim.Value);
        marker.ExpiresAt = _timing.CurTime + ent.Comp.ReharvestDelay;

        TryReviveHarvestedVictim(victim.Value, args.User);

        _appearance.SetData(ent, SlasherMeatHookVisuals.Status, SlasherMeatHookStatus.Harvested);
        _pointLight.SetColor(ent, ent.Comp.HarvestedLightColor);

        var fragment = Spawn(ent.Comp.SoulFragmentPrototype, Transform(ent).Coordinates);
        _hands.TryPickup(args.User, fragment, handsComp: hands);

        // Normal species should carry the fragment with both hands; one-handed-capable species are exempt.
        if (!HasComp<CanWieldOneHandedComponent>(args.User)
            && !HasComp<CarrierOneHandComponent>(args.User)
            && TryComp<WieldableComponent>(fragment, out var wieldable))
        {
            _wield.TryWield(fragment, wieldable, args.User);
        }

        _popup.PopupEntity(Loc.GetString("slasher-meathook-harvest-success"), args.User, args.User, PopupType.MediumCaution);
    }

    /// <summary>
    /// Soul fragments collapse once they leave the user's hands.
    /// </summary>
    private void OnSoulFragmentLeftHands(Entity<SlasherSoulFragmentComponent> ent, ref GotUnequippedHandEvent args)
    {
        _popup.PopupEntity(Loc.GetString("slasher-soul-fragment-fades"), args.User, args.User, PopupType.MediumCaution);
        QueueDel(ent);
    }

    /// <summary>
    /// Appends harvested-state examine text for entities under harvest lockout.
    /// </summary>
    /// <param name="ent">Entity with harvested lockout component.</param>
    /// <param name="args">Examine event data.</param>
    private void OnHarvestedExamined(Entity<SharedSlasherSoulHarvestedComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("slasher-meathook-harvested-examine"));
    }

    /// <summary>
    /// Verifies victim is still hooked on the expected spike and removes healing state otherwise.
    /// </summary>
    /// <param name="victim">Victim entity to validate.</param>
    /// <param name="healing">Runtime healing state for this victim.</param>
    /// <returns>True when victim is still hooked on the expected hook.</returns>
    private bool IsStillHookedVictim(EntityUid victim, SlasherMeatHookHealingComponent healing)
    {
        if (!TryComp<SlasherMeatHookSpikeComponent>(healing.Hook, out var spike) || spike.BodyContainer.ContainedEntity != victim)
        {
            RemCompDeferred<SlasherMeatHookHealingComponent>(victim);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Applies one damage-healing tick toward configured target damage.
    /// </summary>
    /// <param name="uid">Victim being healed.</param>
    /// <param name="healing">Runtime healing configuration.</param>
    /// <param name="damageable">Damageable component for the victim.</param>
    private void ApplyDamageHealing(EntityUid uid, SlasherMeatHookHealingComponent healing, DamageableComponent damageable)
    {
        if (damageable.TotalDamage < healing.TargetDamage)
            return;

        var excess = damageable.TotalDamage - healing.TargetDamage;
        var healAmount = FixedPoint2.Min(healing.HealPerTick, excess);

        // Heal evenly across all damage types at once — this respects the cap
        // and avoids wasting budget on empty groups from the per-group loop.
        _damageable.HealEvenly((uid, damageable), -healAmount, null, healing.Hook);
    }

    /// <summary>
    /// Checks whether the user has enough free hands to perform a harvest.
    /// </summary>
    private bool CanStartHarvest(EntityUid user, out HandsComponent? hands)
    {
        hands = null;
        if (!TryComp(user, out hands))
            return false;

        var handsRequired = HasComp<CanWieldOneHandedComponent>(user) || HasComp<CarrierOneHandComponent>(user)
            ? 1
            : 2;

        return _hands.GetEmptyHandCount((user, hands)) >= handsRequired;
    }

    /// <summary>
    /// Revives a harvested victim to critical condition and prompts their player to return to the body.
    /// </summary>
    private void TryReviveHarvestedVictim(EntityUid victim, EntityUid origin)
    {
        if (!_mobState.IsDead(victim)
            || !TryComp(victim, out MobStateComponent? mobState)
            || !TryComp(victim, out DamageableComponent? damageable)
            || !_mobThreshold.TryGetThresholdForState(victim, MobState.Dead, out var deadThreshold))
        {
            return;
        }

        var reviveThreshold = deadThreshold.Value - 1;
        if (damageable.TotalDamage >= reviveThreshold)
        {
            var reviveHeal = damageable.TotalDamage - reviveThreshold;
            _damageable.HealEvenly(victim, -reviveHeal, null, origin);
        }

        _mobState.ChangeMobState(victim, MobState.Critical, mobState, origin);

        if (_mind.TryGetMind(victim, out _, out var mind)
            && _player.TryGetSessionById(mind.UserId, out var session)
            && mind.CurrentEntity != victim)
        {
            _euiManager.OpenEui(new ReturnToBodyEui(mind, _mind, _player), session);
        }
    }

    /// <summary>
    /// Soul harvesting is allowed from living victims and recently dead victims.
    /// </summary>
    private bool CanHarvestSoul(EntityUid victim)
    {
        if (!_mobState.IsDead(victim))
            return true;

        if (!TryComp<SlasherRecentDeathComponent>(victim, out var recentDeath) || recentDeath.TimeOfDeath == null)
            return false;

        return _timing.CurTime - recentDeath.TimeOfDeath.Value <= recentDeath.RecentDeathWindow;
    }

    /// <summary>
    /// Resolves meathook placement/runtime configuration from the meathook prototype.
    /// Falls back to component defaults if the prototype component cannot be read.
    /// </summary>
    private SlasherMeatHookComponent GetMeathookConfig()
    {
        if (_prototypeManager.Index<EntityPrototype>(SlasherMeathookProto)
            .TryGetComponent<SlasherMeatHookComponent>(out var config, EntityManager.ComponentFactory))
        {
            return config;
        }

        return new SlasherMeatHookComponent();
    }

    /// <summary>
    /// Checks whether a coordinate is a valid tile for meathook placement.
    /// </summary>
    /// <param name="mapCoords">Requested map coordinates from world targeting.</param>
    /// <param name="spawnCoords">Resolved snapped local spawn coordinates when valid.</param>
    /// <returns>True when placement coordinates are valid.</returns>
    private bool TryGetPlacementCoordinates(MapCoordinates mapCoords, out EntityCoordinates spawnCoords, out PlacementFailureReason failure)
    {
        spawnCoords = EntityCoordinates.Invalid;
        failure = PlacementFailureReason.None;

        if (!_mapManager.TryFindGridAt(mapCoords, out var gridUid, out var gridComp))
        {
            failure = PlacementFailureReason.NoGrid;
            return false;
        }

        var tile = _map.WorldToTile(gridUid, gridComp, mapCoords.Position);
        if (!_map.TryGetTileRef(gridUid, gridComp, tile, out var tileRef))
        {
            failure = PlacementFailureReason.NoGrid;
            return false;
        }

        if (tileRef.Tile.IsEmpty || _turf.IsSpace(tileRef))
        {
            failure = PlacementFailureReason.EmptyOrSpace;
            return false;
        }

        if (_turf.IsTileBlocked(tileRef, CollisionGroup.Impassable))
        {
            failure = PlacementFailureReason.Blocked;
            return false;
        }

        spawnCoords = _map.GridTileToLocal(gridUid, gridComp, tile).SnapToGrid();
        return true;
    }

    /// <summary>
    /// Shows placement failure feedback for meathook placement attempts.
    /// </summary>
    private void PopupPlacementFailure(EntityUid performer, PlacementFailureReason failure, string fallbackLocale)
    {
        var msg = failure switch
        {
            PlacementFailureReason.NoGrid => "slasher-place-invalid-no-grid",
            PlacementFailureReason.EmptyOrSpace => "slasher-place-invalid-space",
            PlacementFailureReason.Blocked => "slasher-place-invalid-blocked",
            _ => fallbackLocale,
        };

        _popup.PopupEntity(Loc.GetString(msg), performer, performer, PopupType.MediumCaution);
    }

}
