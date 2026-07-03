using Content.Server._Impstation.GameTicking.Rules;
using Content.Server._Impstation.GameTicking.Rules.Components;
using Content.Server._Impstation.Ghost;
using Content.Server._Impstation.Slasher.Components;
using Content.Server.AlertLevel;
using Content.Server.Anomaly;
using Content.Server.Anomaly.Components;
using Content.Server.GameTicking;
using Content.Server.Pinpointer;
using Content.Server.Station.Systems;
using Content.Shared.Anomaly;
using Content.Shared.Anomaly.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Eye;
using Content.Shared.Flash.Components;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Pinpointer;
using Content.Shared.Popups;
using Content.Shared._Impstation.Slasher;
using Content.Shared._Impstation.Slasher.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Impstation.Slasher;

/// <summary>
/// Handles Slasher effigy placement, soul-fragment insertion, effigy-destruction consequences,
/// and keeping effigy-pinpointers in sync with rule state.
/// </summary>
public sealed class SlasherEffigySystem : EntitySystem
{
    private readonly ISawmill _sawmill = Logger.GetSawmill("slasher.effigy");

    /// <summary>
    /// Enumeration values for PlacementFailureReason.
    /// </summary>
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private enum PlacementFailureReason
    {
        None,
        NoGrid,
        EmptyOrSpace,
        Blocked,
    }

    // Matches Funky Station Cosmic Cult pattern: hidden entity uses only the role-specific flag bit.
    // PVS check is (viewerMask & entityMetaMask) == entityMetaMask, where entityMetaMask = 1 | Layer.
    // A Slasher viewer (mask = Normal|Slasher = 17) can see an entity with MetaMask 17 (layer 16).
    /// <summary>
    /// Visibility layer used while the effigy is hidden from non-Slashers.
    /// </summary>
    private static readonly ushort HiddenEffigyVisibilityLayer = SlasherEffigyComponent.LayerMask;

    /// <summary>
    /// Prototype ID for soul fragments fed into the effigy.
    /// </summary>
    private static readonly EntProtoId SlasherSoulFragmentProto = "SlasherSoulFragment";
    /// <summary>
    /// Prototype ID for the meathook-placement action granted after effigy placement.
    /// </summary>
    private static readonly EntProtoId SlasherPlaceMeathookActionProto = "ActionSlasherPlaceMeathook";

    /// <summary>
    /// Prototype ID for the effigy-placement action, re-granted after effigy restoration.
    /// </summary>
    private static readonly EntProtoId SlasherPlaceEffigyActionProto = "ActionSlasherPlaceEffigy";

    /// <summary>
    /// Tracks last recorded damage for each effigy to detect threshold crossings.
    /// </summary>
    private readonly Dictionary<EntityUid, FixedPoint2> _effigyLastDamage = new();

    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly AnomalySystem _anomaly = default!;
    [Dependency] private readonly AnomalyScannerSystem _anomalyScanner = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly PinpointerSystem _pinpointer = default!;
    [Dependency] private readonly SlasherEffigyPulseSystem _pulse = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SlasherRuleSystem _rule = default!;
    [Dependency] private readonly AlertLevelSystem _alert = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly VisibilitySystem _visibility = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <summary>
    /// Subscribes effigy placement, insertion, damage filtering, and lifecycle handlers.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlasherEffigyComponent, ComponentStartup>(OnEffigyStartup);
        SubscribeLocalEvent<SlasherEffigyComponent, TryScanEntityEvent>(OnEffigyScanValidate);
        SubscribeLocalEvent<SlasherEffigyComponent, ScannerBuildMessageEvent>(OnEffigyScannerBuildMessage);
        SubscribeLocalEvent<SlasherPlaceEffigyActionComponent, ActionAttemptEvent>(OnActionAttempt);
        SubscribeLocalEvent<SlasherPlaceEffigyActionComponent, SlasherPlaceEffigyEvent>(OnPlaceEffigy);
        SubscribeLocalEvent<SlasherRoleComponent, SlasherPlaceEffigyDoAfterEvent>(OnPlaceEffigyDoAfter);

        SubscribeLocalEvent<SlasherEffigyComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<SlasherEffigyComponent, DamageModifyEvent>(OnDamageModify);
        SubscribeLocalEvent<SlasherEffigyComponent, DamageChangedEvent>(OnEffigyDamageChanged);
        SubscribeLocalEvent<SlasherEffigyComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
        SubscribeLocalEvent<SlasherEffigyComponent, SlasherFeedEffigyDoAfterEvent>(OnFeedEffigyDoAfter);
        SubscribeLocalEvent<FlashedComponent, ComponentInit>(OnFlashed);
        SubscribeLocalEvent<SlasherEffigyComponent, EntityTerminatingEvent>(OnEffigyTerminating);

        SubscribeLocalEvent<SlasherSoulFragmentComponent, UseInHandEvent>(OnSoulFragmentUseInHand);
        SubscribeLocalEvent<SlasherSoulFragmentComponent, SlasherRestoreEffigyDoAfterEvent>(OnRestoreEffigyDoAfter);
    }

    /// <summary>
    /// Ticks effigy runtime state and re-hides revealed effigies once their vulnerability window expires.
    /// </summary>
    /// <param name="frameTime">Frame delta time in seconds.</param>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        // Hide any revealed effigy whose vulnerability timer has elapsed.
        var query = EntityQueryEnumerator<SlasherEffigyComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.PermanentlyRevealed)
                continue;

            if (comp.Revealed && comp.VulnerableUntil.HasValue && now >= comp.VulnerableUntil.Value)
                HideEffigy((uid, comp));
        }
    }

    /// <summary>
    /// Syncs all Slasher effigy pinpointers to the current active effigy target.
    /// </summary>
    /// <param name="activeEffigy">Current active effigy entity, or null when no effigy should be tracked.</param>
    public void SyncEffigyPinpointers(EntityUid? activeEffigy)
    {
        EntityUid? target = activeEffigy is { } effigy && Exists(effigy) ? effigy : null;
        UpdateEffigyPinpointers(target);
    }

    /// <summary>
    /// Accepts or rejects an anomaly scanner scan attempt on this effigy.
    /// Hidden effigies reject with a popup; revealed effigies accept.
    /// </summary>
    private static void OnEffigyScanValidate(Entity<SlasherEffigyComponent> ent, ref TryScanEntityEvent args)
    {
        if (!ent.Comp.Revealed)
        {
            args.RejectionLocale = "anomaly-scanner-effigy-hidden";
            return;
        }

        args.Accepted = true;
    }

    /// <summary>
    /// Supplies the anomaly scanner with a custom formatted message for this effigy.
    /// </summary>
    private void OnEffigyScannerBuildMessage(Entity<SlasherEffigyComponent> ent, ref ScannerBuildMessageEvent args)
    {
        args.Message = BuildEffigyScannerMessage(ent);
        // Supply VulnerableUntil as the countdown so the scanner UI shows a live-ticking timer.
        if (ent.Comp.Revealed && ent.Comp.VulnerableUntil.HasValue)
            args.NextPulseTime = ent.Comp.VulnerableUntil.Value;
    }

    /// <summary>
    /// Builds the anomaly-scanner-format readout for a revealed effigy.
    /// Severity mirrors Slasher rule progress; all other fields show ERROR placeholders except containment particle.
    /// </summary>
    private FormattedMessage BuildEffigyScannerMessage(Entity<SlasherEffigyComponent> effigy)
    {
        var msg = new FormattedMessage();

        if (!effigy.Comp.Revealed)
        {
            msg.AddMarkupOrThrow(Loc.GetString("anomaly-scanner-effigy-hidden-readout"));
            return msg;
        }

        var severityPercent = GetEffigySeverityPercent(effigy.Comp);
        msg.AddMarkupOrThrow(Loc.GetString("anomaly-scanner-severity-percentage", ("percent", severityPercent.ToString("P"))));
        msg.PushNewline();

        msg.AddMarkupOrThrow(Loc.GetString("anomaly-scanner-stability-unknown"));
        msg.PushNewline();

        msg.AddMarkupOrThrow(Loc.GetString("anomaly-scanner-point-output-unknown"));
        msg.PushNewline();
        msg.PushNewline();

        msg.AddMarkupOrThrow(Loc.GetString("anomaly-scanner-particle-readout"));
        msg.PushNewline();

        msg.AddMarkupOrThrow(Loc.GetString("anomaly-scanner-particle-danger-unknown"));
        msg.PushNewline();
        msg.AddMarkupOrThrow(Loc.GetString("anomaly-scanner-particle-unstable-unknown"));
        msg.PushNewline();
        msg.AddMarkupOrThrow(Loc.GetString("anomaly-scanner-particle-containment",
            ("type", _anomaly.GetParticleLocale(effigy.Comp.RequiredContainmentParticle))));
        msg.PushNewline();
        msg.AddMarkupOrThrow(Loc.GetString("anomaly-scanner-particle-transformation-unknown"));

        msg.PushNewline();
        msg.PushNewline();
        msg.AddMarkupOrThrow(Loc.GetString("anomaly-behavior-title"));
        msg.PushNewline();
        msg.AddMarkupOrThrow(Loc.GetString("anomaly-behavior-unknown"));

        return msg;
    }

    /// <summary>
    /// Returns an anomaly-style [0,1] severity fraction sourced from the active Slasher rule progress.
    /// Falls back to effigy insertion count when no rule is active.
    /// </summary>
    private float GetEffigySeverityPercent(SlasherEffigyComponent effigy)
    {
        if (_rule.TryGetActiveRule(out var rule) && rule.Comp.TargetInsertions > 0)
            return Math.Clamp((float)rule.Comp.FragmentInsertions / rule.Comp.TargetInsertions, 0f, 1f);

        return Math.Clamp(effigy.Insertions / 10f, 0f, 1f);
    }

    /// <summary>
    /// Applies the correct hidden/revealed physics and visual state when an effigy starts up.
    /// </summary>
    private void OnEffigyStartup(Entity<SlasherEffigyComponent> ent, ref ComponentStartup args)
    {
        EnsureValidRequiredParticle(ent);
        ApplyEffigyState(ent, announceReveal: false);
    }

    /// <summary>
    /// Cancels placement action attempts once an effigy has already been placed or remains active.
    /// </summary>
    /// <param name="ent">Effigy-placement action entity and component data.</param>
    /// <param name="args">Action-attempt event data.</param>
    private void OnActionAttempt(Entity<SlasherPlaceEffigyActionComponent> ent, ref ActionAttemptEvent args)
    {
        if (!_rule.TryGetActiveRule(out var rule))
        {
            args.Cancelled = true;
            return;
        }

        if (rule.Comp.EffigyPlacedEver || (rule.Comp.ActiveEffigy is { } active && Exists(active)))
            args.Cancelled = true;
    }

    /// <summary>
    /// Places the Slasher effigy at a valid world target and synchronizes rule/pinpointer state.
    /// </summary>
    /// <param name="ent">Effigy-placement action entity and component data.</param>
    /// <param name="args">World-target placement event data.</param>
    private void OnPlaceEffigy(Entity<SlasherPlaceEffigyActionComponent> ent, ref SlasherPlaceEffigyEvent args)
    {
        if (args.Handled)
            return;

        if (!HasComp<SlasherRoleComponent>(args.Performer))
            return;

        if (!_rule.TryGetActiveRule(out var rule))
        {
            _popup.PopupEntity(Loc.GetString("slasher-effigy-place-no-rule"), args.Performer, args.Performer, PopupType.MediumCaution);
            return;
        }

        if (rule.Comp.EffigyPlacedEver)
        {
            _popup.PopupEntity(Loc.GetString("slasher-effigy-place-already"), args.Performer, args.Performer, PopupType.MediumCaution);
            return;
        }

        if (rule.Comp.ActiveEffigy is { } activeEffigy && Exists(activeEffigy))
        {
            _popup.PopupEntity(Loc.GetString("slasher-effigy-place-active"), args.Performer, args.Performer, PopupType.MediumCaution);
            return;
        }

        var mapCoords = _transform.ToMapCoordinates(args.Target);
        if (!TryGetPlacementCoordinates(mapCoords, out var spawnCoords, out var failure))
        {
            PopupPlacementFailure(args.Performer, failure, "slasher-effigy-place-invalid");
            return;
        }

        if (!Transform(args.Performer).Coordinates.TryDistance(EntityManager, spawnCoords, out var placementDistance)
            || placementDistance > ent.Comp.PlacementRange)
        {
            _popup.PopupEntity(Loc.GetString("slasher-place-invalid-too-far", ("distance", ent.Comp.PlacementRange)),
                args.Performer,
                args.Performer,
                PopupType.MediumCaution);
            return;
        }

        var doAfterEvent = new SlasherPlaceEffigyDoAfterEvent(GetNetCoordinates(spawnCoords), GetNetEntity(ent.Owner));
        var doAfterArgs = new DoAfterArgs(EntityManager,
            args.Performer,
            ent.Comp.PlacementDoAfterDelay,
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
    /// Finalizes effigy placement after the channeling do-after completes.
    /// </summary>
    private void OnPlaceEffigyDoAfter(Entity<SlasherRoleComponent> ent, ref SlasherPlaceEffigyDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        var actionEntity = GetEntity(args.ActionEntity);
        if (!TryComp(actionEntity, out SlasherPlaceEffigyActionComponent? placeComp))
            return;

        if (!_rule.TryGetActiveRule(out var rule))
            return;

        if (rule.Comp.EffigyPlacedEver)
        {
            _popup.PopupEntity(Loc.GetString("slasher-effigy-place-already"), ent, ent, PopupType.MediumCaution);
            return;
        }

        if (rule.Comp.ActiveEffigy is { } activeEffigy && Exists(activeEffigy))
        {
            _popup.PopupEntity(Loc.GetString("slasher-effigy-place-active"), ent, ent, PopupType.MediumCaution);
            return;
        }

        var requestedCoords = GetCoordinates(args.TargetCoordinates);
        var mapCoords = _transform.ToMapCoordinates(requestedCoords);
        if (!TryGetPlacementCoordinates(mapCoords, out var spawnCoords, out var failure))
        {
            PopupPlacementFailure(ent.Owner, failure, "slasher-effigy-place-invalid");
            return;
        }

        if (!Transform(ent).Coordinates.TryDistance(EntityManager, spawnCoords, out var placementDistance)
            || placementDistance > placeComp.PlacementRange)
        {
            _popup.PopupEntity(Loc.GetString("slasher-place-invalid-too-far", ("distance", placeComp.PlacementRange)),
                ent,
                ent,
                PopupType.MediumCaution);
            return;
        }

        // Spawn hidden and initialize runtime-only state before wiring it into rule tracking.
        var effigy = Spawn(placeComp.EffigyPrototype, spawnCoords);

        if (TryComp<SlasherEffigyComponent>(effigy, out var effigyComp))
        {
            effigyComp.Insertions = 0;
            effigyComp.Revealed = false;
            effigyComp.PermanentlyRevealed = false;
            effigyComp.VulnerableUntil = null;
            RerollRequiredParticle((effigy, effigyComp));
            Dirty(effigy, effigyComp);
        }

        SetEffigyHiddenLayer(effigy);
        _appearance.SetData(effigy, SlasherEffigyVisuals.Status, SlasherEffigyStatus.Hidden);

        // Mirror placement into the active slasher rule and update crew-side tracking tools.
        rule.Comp.ActiveEffigy = effigy;
        rule.Comp.EffigyPlacedEver = true;
        rule.Comp.EffigyDestroyed = false;
        rule.Comp.FragmentInsertions = 0;
        _sawmill.Info($"Slasher {ToPrettyString(ent.Owner)} placed effigy {ToPrettyString(effigy)} at {spawnCoords}.");
        UpdateEffigyPinpointers(effigy);
        UpdateLocateEffigyAvailability(effigy);
        ConsumePlaceEffigyAction(ent, actionEntity);
        EnsureMeathookActionGranted(ent);
    }

    /// <summary>
    /// Removes the one-shot place-effigy action after the player successfully places an effigy.
    /// </summary>
    /// <param name="performer">Entity performing this action.</param>
    /// <param name="actionEntity">Action entity to revoke and unregister.</param>
    private void ConsumePlaceEffigyAction(EntityUid performer, EntityUid actionEntity)
    {
        _actions.RemoveAction(performer, actionEntity);

        if (!TryComp<SlasherRoleComponent>(performer, out var role))
            return;

        role.ActionEntities.Remove(actionEntity);
    }

    /// <summary>
    /// Resolves a valid effigy placement coordinate on a non-space, non-blocked tile.
    /// </summary>
    /// <param name="mapCoords">Requested map coordinates from world targeting.</param>
    /// <param name="spawnCoords">Resolved snapped local coordinates when valid.</param>
    /// <returns>True when a valid placement tile is found.</returns>
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
    /// Shows placement failure feedback for effigy placement attempts.
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

    /// <summary>
    /// Ensures the Slasher has the meathook placement action after placing an effigy.
    /// </summary>
    /// <param name="performer">Slasher performer to validate and grant action to.</param>
    private void EnsureMeathookActionGranted(EntityUid performer)
    {
        if (!TryComp<SlasherRoleComponent>(performer, out var role))
            return;

        foreach (var action in role.ActionEntities)
        {
            if (!TryComp(action, out MetaDataComponent? meta) || meta.EntityPrototype == null)
                continue;

            if (meta.EntityPrototype.ID == SlasherPlaceMeathookActionProto.Id)
                return;
        }

        EntityUid? actionEntity = null;
        if (_actions.AddAction(performer, ref actionEntity, SlasherPlaceMeathookActionProto)
            && actionEntity is { } addedAction)
        {
            role.ActionEntities.Add(addedAction);
        }
    }

    /// <summary>
    /// Starts fragment insertion do-after when a Slasher uses a soul fragment on an active effigy.
    /// </summary>
    /// <param name="ent">Effigy entity and component data.</param>
    /// <param name="args">After-interact-using event data.</param>
    private void OnAfterInteractUsing(Entity<SlasherEffigyComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!HasComp<SlasherRoleComponent>(args.User))
            return;

        if (!TryComp(args.Used, out MetaDataComponent? meta) || meta.EntityPrototype?.ID != SlasherSoulFragmentProto.Id)
        {
            _popup.PopupEntity(Loc.GetString("slasher-effigy-insert-need-fragment"), args.User, args.User, PopupType.MediumCaution);
            return;
        }

        if (!_rule.TryGetActiveRule(out var rule)
            || rule.Comp.ActiveEffigy is not { } activeEffigy
            || !Exists(activeEffigy))
            return;

        // The reveal window begins as soon as the insertion attempt starts, not on completion.
        var doAfterArgs = new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.InsertDoAfterDuration,
            new SlasherFeedEffigyDoAfterEvent(),
            ent,
            target: ent,
            used: args.Used)
        {
            BreakOnMove = ent.Comp.BreakOnMove,
            BreakOnHandChange = ent.Comp.BreakOnHandInteract,
            BreakOnDamage = true,
            NeedHand = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfterArgs))
            return;

        // Refresh the vulnerability timer so repeated insertions extend the reveal window.
        if (!ent.Comp.PermanentlyRevealed)
        {
            ent.Comp.VulnerableUntil = _timing.CurTime + ent.Comp.VulnerabilityDuration;
            if (!ent.Comp.Revealed)
                RevealEffigy(ent);
        }

        Dirty(ent);
        args.Handled = true;
    }

    /// <summary>
    /// Completes soul-fragment insertion and applies related progress/healing effects.
    /// </summary>
    /// <param name="ent">Effigy entity and component data.</param>
    /// <param name="args">Do-after completion data for fragment insertion.</param>
    private void OnFeedEffigyDoAfter(Entity<SlasherEffigyComponent> ent, ref SlasherFeedEffigyDoAfterEvent args)
    {
        if (args.Cancelled || args.Used == null)
            return;

        if (!_rule.TryGetActiveRule(out var rule)
            || rule.Comp.ActiveEffigy is not { } activeEffigy
            || !Exists(activeEffigy))
            return;

        if (!TryComp(args.Used, out MetaDataComponent? meta) || meta.EntityPrototype?.ID != SlasherSoulFragmentProto.Id)
            return;

        // Keep the networked effigy state in sync with the authoritative slasher rule progress.
        rule.Comp.FragmentInsertions++;
        var insertions = rule.Comp.FragmentInsertions;
        var target = rule.Comp.TargetInsertions;

        QueueDel(args.Used.Value);
        ent.Comp.Insertions = insertions;
        Dirty(ent);

        ApplyFragmentHeal(args.User, ent.Comp.HealPerFragment, ent.Comp.HealDamageGroups);

        _popup.PopupEntity(
            Loc.GetString("slasher-effigy-crackle-shift"),
            ent,
            Filter.Pvs(ent, entityManager: EntityManager),
            true,
            PopupType.LargeCaution);

        // Roll pulse effects immediately when an insertion successfully completes.
        _pulse.TriggerHidePulse(ent);

        var finalPhaseThreshold = Math.Max(0, target - ent.Comp.FinalPhaseFragmentsRemaining);
        if (!rule.Comp.FinalPhaseTriggered && insertions >= finalPhaseThreshold)
        {
            rule.Comp.FinalPhaseTriggered = true;
            TriggerFinalPhase(ent);
        }

        if (insertions >= target && !rule.Comp.VictoryTriggered)
        {
            rule.Comp.VictoryTriggered = true;
            rule.Comp.RoundEndOutcome = SlasherRoundEndOutcome.SlasherMajor;
            _popup.PopupEntity(Loc.GetString("slasher-effigy-insert-complete"), args.User, args.User, PopupType.LargeCaution);
            // Small delay so the final-insertion popup is visible before effects fire.
            Timer.Spawn(ent.Comp.VictorySequenceDelay, () =>
            {
                var ev = new SlasherVictoryTimerFiredEvent(rule.Owner);
                RaiseLocalEvent(rule.Owner, ref ev);
            });
        }
    }

    /// <summary>
    /// Applies configured post-insertion healing to the slasher user.
    /// </summary>
    /// <param name="user">Slasher who completed the insertion.</param>
    /// <param name="healPerFragment">Total heal amount to split across groups.</param>
    /// <param name="healGroups">Damage groups eligible for split healing.</param>
    private void ApplyFragmentHeal(EntityUid user, float healPerFragment, ProtoId<DamageGroupPrototype>[] healGroups)
    {
        if (healPerFragment <= 0f || healGroups.Length == 0 || !TryComp<DamageableComponent>(user, out var damageable))
            return;

        // Split the configured healing evenly across the configured damage groups.
        var healTotal = FixedPoint2.New(healPerFragment);
        var healPerGroup = healTotal / healGroups.Length;
        foreach (var group in healGroups)
            _damageable.HealEvenly((user, damageable), -healPerGroup, group, user);
    }

    /// <summary>
    /// Applies reveal-window disruption damage when a matching anomalous particle projectile collides with the effigy.
    /// </summary>
    /// <param name="ent">Effigy entity and component data.</param>
    /// <param name="args">Collision-start event data.</param>
    private void OnStartCollide(Entity<SlasherEffigyComponent> ent, ref StartCollideEvent args)
    {
        if (!ent.Comp.Revealed)
            return;

        if (!TryComp<AnomalousParticleComponent>(args.OtherEntity, out var particleComp))
            return;

        if (args.OtherFixtureId != particleComp.FixtureId)
            return;

        if (particleComp.ParticleType != ent.Comp.RequiredContainmentParticle)
            return;

        var damage = new DamageSpecifier();
        damage.DamageDict["Structural"] = FixedPoint2.New(ent.Comp.MatchingParticleDamage);
        _damageable.TryChangeDamage(ent.Owner, damage, ignoreResistances: true, origin: args.OtherEntity);
    }

    /// <summary>
    /// Filters incoming damage so only configured damage types can hurt revealed effigies.
    /// Hidden effigies are fully immune.
    /// </summary>
    /// <param name="ent">Effigy entity and component data.</param>
    /// <param name="args">Damage-modify event data.</param>
    private static void OnDamageModify(Entity<SlasherEffigyComponent> ent, ref DamageModifyEvent args)
    {
        if (!ent.Comp.Revealed)
        {
            args.Damage = new();
            return;
        }

        var filtered = new DamageSpecifier();
        foreach (var (type, amount) in args.Damage.DamageDict)
        {
            if (!ent.Comp.RevealedDamageTypes.Contains(type))
                continue;

            filtered.DamageDict[type] = amount;
        }

        args.Damage = filtered;
    }

    /// <summary>
    /// Detects when revealed effigy damage crosses the half-health threshold (200 total damage).
    /// Sends a warning popup to all Slashers without spamming on repeated damage.
    /// </summary>
    private void OnEffigyDamageChanged(EntityUid uid, SlasherEffigyComponent ent, DamageChangedEvent args)
    {
        if (!ent.Revealed || args.DamageDelta is null)
            return;

        // Get previously stored damage or initialize to 0
        if (!_effigyLastDamage.TryGetValue(uid, out var lastDamage))
            lastDamage = FixedPoint2.Zero;

        var currentDamage = args.Damageable.TotalDamage;

        var warningThreshold = FixedPoint2.New(ent.DamagedWarningThreshold);

        // Trigger warning when crossing configured threshold upward.
        if (lastDamage < warningThreshold && currentDamage >= warningThreshold)
        {
            var slashers = EntityQueryEnumerator<SlasherRoleComponent>();
            while (slashers.MoveNext(out var slasherUid, out _))
                _popup.PopupEntity(Loc.GetString("slasher-effigy-damaged-warning"), slasherUid, slasherUid, PopupType.LargeCaution);
        }

        _effigyLastDamage[uid] = currentDamage;
    }

    /// <summary>
    /// Marks effigy destruction in rule state and applies failure state to all Slashers.
    /// </summary>
    /// <param name="ent">Effigy entity and component data.</param>
    /// <param name="args">Entity-terminating event data.</param>
    private void OnEffigyTerminating(Entity<SlasherEffigyComponent> ent, ref EntityTerminatingEvent args)
    {
        _effigyLastDamage.Remove(ent.Owner);
        ClearScannersTrackingEffigy(ent.Owner);

        if (!_rule.TryGetActiveRule(out var rule)
            || rule.Comp.ActiveEffigy != ent.Owner
            || rule.Comp.EffigyDestroyed)
            return;

        // Destroying the active effigy permanently fails the Slasher objective.
        rule.Comp.EffigyDestroyed = true;
        rule.Comp.ActiveEffigy = null;
        UpdateEffigyPinpointers(null);
        UpdateLocateEffigyAvailability(null);

        // Tell each Slasher their effigy was destroyed.
        var slashers = EntityQueryEnumerator<SlasherRoleComponent>();
        while (slashers.MoveNext(out var uid, out _))
            _popup.PopupEntity(Loc.GetString("slasher-effigy-destroyed"), uid, uid, PopupType.LargeCaution);

        _audio.PlayPvs(ent.Comp.EffigyDestroyedSound, ent, AudioParams.Default);

        // Send the station-wide warning before the effigy finishes terminating.
        var stationFilter = _station.GetInOwningStation(ent.Owner);
        _popup.PopupEntity(Loc.GetString("slasher-effigy-destroyed-others"), ent,
            stationFilter, true, PopupType.LargeCaution);

        _sawmill.Info($"Slasher effigy was destroyed: {ToPrettyString(ent.Owner)}");

        slashers = EntityQueryEnumerator<SlasherRoleComponent>();
        while (slashers.MoveNext(out var uid, out _))
        {
            if (TerminatingOrDeleted(uid))
                continue;

            EnsureComp<SlasherEffigyFailureComponent>(uid);
        }
    }

    /// <summary>
    /// Reveals the effigy when it is flashed and refreshes its vulnerability window.
    /// Always resets reveal state (particle requirement, visuals) to ensure consistent behavior on repeated flashes.
    /// </summary>
    private void OnFlashed(Entity<FlashedComponent> ent, ref ComponentInit args)
    {
        if (!TryComp<SlasherEffigyComponent>(ent, out var effigy) || effigy.PermanentlyRevealed)
            return;

        effigy.VulnerableUntil = _timing.CurTime + effigy.VulnerabilityDuration;
        RevealEffigy((ent, effigy));

        Dirty(ent, effigy);
    }

    /// <summary>
    /// Updates all effigy pinpointers to track the supplied target or clear tracking when null.
    /// </summary>
    /// <param name="target">Effigy target to track, or null to disable tracking.</param>
    private void UpdateEffigyPinpointers(EntityUid? target)
    {
        var query = EntityQueryEnumerator<SlasherEffigyPinpointerComponent, PinpointerComponent>();
        while (query.MoveNext(out var uid, out _, out var pinpointer))
        {
            _pinpointer.SetTarget(uid, target, pinpointer);
            _pinpointer.SetActive(uid, target != null, pinpointer);
        }
    }

    /// <summary>
    /// Sets effigy state to revealed and applies revealed visuals/visibility behavior.
    /// </summary>
    /// <param name="ent">Effigy entity and component data.</param>
    private void RevealEffigy(Entity<SlasherEffigyComponent> ent)
    {
        RerollRequiredParticle(ent);
        ent.Comp.Revealed = true;
        Dirty(ent);
        ApplyEffigyState(ent, announceReveal: true);
    }

    /// <summary>
    /// Re-hides an effigy after the vulnerability window closes.
    /// </summary>
    /// <param name="ent">Effigy entity and component data.</param>
    private void HideEffigy(Entity<SlasherEffigyComponent> ent)
    {
        _popup.PopupEntity(Loc.GetString("slasher-effigy-hide"), ent,
            Filter.Pvs(ent, entityManager: EntityManager), true, PopupType.LargeCaution);

        ent.Comp.Revealed = false;
        ent.Comp.VulnerableUntil = null;
        Dirty(ent);

        ClearScannersTrackingEffigy(ent.Owner);

        ApplyEffigyState(ent, announceReveal: false);

        // Pulses are rolled on successful insertion completion, not on hide.
    }

    /// <summary>
    /// Clears anomaly scanner targets that are currently tracking this effigy.
    /// </summary>
    private void ClearScannersTrackingEffigy(EntityUid effigy)
    {
        var scanners = EntityQueryEnumerator<AnomalyScannerComponent>();
        while (scanners.MoveNext(out var scannerUid, out var scanner))
        {
            if (scanner.ScannedEntity != effigy)
                continue;

            _anomalyScanner.ClearScanTarget(scannerUid, scanner);
        }
    }

    /// <summary>
    /// Applies revealed or hidden effigy state, including visibility, collision, and announcement behavior.
    /// </summary>
    /// <param name="ent">Effigy entity and component data.</param>
    /// <param name="announceReveal">Whether reveal should announce to nearby viewers.</param>
    private void ApplyEffigyState(Entity<SlasherEffigyComponent> ent, bool announceReveal)
    {
        if (ent.Comp.Revealed)
        {
            // Revealed effigies are visible and collidable so anomalous particles can disrupt them.
            if (TryComp<VisibilityComponent>(ent, out var visibility))
                _visibility.SetLayer((ent.Owner, visibility), (ushort)VisibilityFlags.Normal);

            _physics.SetCanCollide(ent.Owner, true);

            _appearance.SetData(ent, SlasherEffigyVisuals.Status, SlasherEffigyStatus.Revealed);

            if (announceReveal)
            {
                _popup.PopupEntity(Loc.GetString("slasher-effigy-revealed"),
                    ent, Filter.Pvs(ent, entityManager: EntityManager), true, PopupType.LargeCaution);
            }

            return;
        }

        // Hidden effigies return to slasher-only visibility and become intangible again.
        SetEffigyHiddenLayer(ent.Owner);
        _appearance.SetData(ent, SlasherEffigyVisuals.Status, SlasherEffigyStatus.Hidden);
    }

    /// <summary>
    /// Applies hidden visibility layer and no-collision state to an effigy entity.
    /// </summary>
    /// <param name="effigy">Effigy entity to update.</param>
    private void SetEffigyHiddenLayer(EntityUid effigy)
    {
        var visibility = EnsureComp<VisibilityComponent>(effigy);
        _visibility.SetLayer((effigy, visibility), HiddenEffigyVisibilityLayer);
        _physics.SetCanCollide(effigy, false);
    }

    /// <summary>
    /// Rolls a new required containment particle type for crewside effigy disruption.
    /// </summary>
    /// <param name="ent">Effigy entity and component data.</param>
    private void RerollRequiredParticle(Entity<SlasherEffigyComponent> ent)
    {
        if (ent.Comp.DisruptionParticleTypes.Length == 0)
        {
            ent.Comp.RequiredContainmentParticle = AnomalousParticleType.Delta;
            return;
        }

        ent.Comp.RequiredContainmentParticle = _random.Pick(ent.Comp.DisruptionParticleTypes);
    }

    /// <summary>
    /// Ensures the effigy's required particle is one of the supported disruption particle types.
    /// </summary>
    /// <param name="ent">Effigy entity and component data.</param>
    private void EnsureValidRequiredParticle(Entity<SlasherEffigyComponent> ent)
    {
        if (Array.IndexOf(ent.Comp.DisruptionParticleTypes, ent.Comp.RequiredContainmentParticle) >= 0)
            return;

        RerollRequiredParticle(ent);
        Dirty(ent);
    }

    /// <summary>
    /// Starts a 30-second do-after when a Slasher uses a soul fragment in hand,
    /// if their effigy has been destroyed and victory has not yet been triggered.
    /// </summary>
    private void OnSoulFragmentUseInHand(Entity<SlasherSoulFragmentComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (!HasComp<SlasherRoleComponent>(args.User))
            return;

        if (!_rule.TryGetActiveRule(out var rule))
            return;

        // Only allow restoration while the effigy is destroyed and victory hasn't fired.
        if (!rule.Comp.EffigyDestroyed || rule.Comp.ActiveEffigy != null || rule.Comp.VictoryTriggered)
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.RestoreEffigyDoAfterDuration,
            new SlasherRestoreEffigyDoAfterEvent(),
            ent.Owner,
            target: args.User,
            used: ent.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        if (_doAfter.TryStartDoAfter(doAfterArgs))
        {
            _popup.PopupEntity(Loc.GetString("slasher-effigy-restore-begin"), args.User, args.User, PopupType.Medium);
            args.Handled = true;
        }
    }

    /// <summary>
    /// Completes effigy restoration: consumes the fragment, resets failed state,
    /// and re-grants the place-effigy action.
    /// </summary>
    private void OnRestoreEffigyDoAfter(Entity<SlasherSoulFragmentComponent> ent, ref SlasherRestoreEffigyDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (!_rule.TryGetActiveRule(out var rule))
            return;

        QueueDel(ent.Owner);

        rule.Comp.EffigyDestroyed = false;
        rule.Comp.ActiveEffigy = null;
        rule.Comp.EffigyPlacedEver = false;
        rule.Comp.FinalPhaseTriggered = false;
        UpdateEffigyPinpointers(null);
        UpdateLocateEffigyAvailability(null);

        // Remove failure state from all Slashers so they can place a new effigy.
        var slashers = EntityQueryEnumerator<SlasherRoleComponent>();
        while (slashers.MoveNext(out var uid, out _))
            RemComp<SlasherEffigyFailureComponent>(uid);

        EnsurePlaceEffigyActionRestored(args.Target ?? args.User);
        _popup.PopupEntity(Loc.GetString("slasher-effigy-restore-complete"), args.Target ?? args.User, args.Target ?? args.User, PopupType.LargeCaution);
    }

    /// <summary>
    /// Re-grants the place-effigy action to the Slasher if they no longer have it.
    /// Mirrors the pattern used by <see cref="EnsureMeathookActionGranted"/>.
    /// </summary>
    private void EnsurePlaceEffigyActionRestored(EntityUid performer)
    {
        if (!TryComp<SlasherRoleComponent>(performer, out var role))
            return;

        foreach (var action in role.ActionEntities)
        {
            if (!TryComp(action, out MetaDataComponent? meta) || meta.EntityPrototype == null)
                continue;

            if (meta.EntityPrototype.ID == SlasherPlaceEffigyActionProto.Id)
                return; // Already has the action.
        }

        EntityUid? actionEntity = null;
        if (_actions.AddAction(performer, ref actionEntity, SlasherPlaceEffigyActionProto)
            && actionEntity is { } addedAction)
        {
            role.ActionEntities.Add(addedAction);
        }
    }

    /// <summary>
    /// Notifies every Slasher whether an effigy locator should currently be available.
    /// </summary>
    private void UpdateLocateEffigyAvailability(EntityUid? effigy)
    {
        var slashers = EntityQueryEnumerator<SlasherRoleComponent>();
        while (slashers.MoveNext(out var uid, out _))
        {
            var ev = new SlasherEffigyLocatorChangedEvent(effigy);
            RaiseLocalEvent(uid, ref ev);
        }
    }

    /// <summary>
    /// Applies the final-two-fragments phase effects:
    /// permanent effigy reveal, Medium vision for all crew, and forced Code White.
    /// </summary>
    private void TriggerFinalPhase(Entity<SlasherEffigyComponent> effigy)
    {
        effigy.Comp.PermanentlyRevealed = true;
        effigy.Comp.Revealed = true;
        effigy.Comp.VulnerableUntil = null;
        Dirty(effigy);
        ApplyEffigyState(effigy, announceReveal: true);

        var stationUid = _station.GetOwningStation(effigy.Owner);
        if (stationUid != null)
            _alert.SetLevel(stationUid.Value, "white", playSound: true, announce: true, force: true);

        // Apply Medium to all crew on the station regardless of health state.
        var crew = EntityQueryEnumerator<ActorComponent>();
        while (crew.MoveNext(out var uid, out _))
        {
            if (HasComp<SlasherRoleComponent>(uid))
                continue;

            if (stationUid != null && _station.GetOwningStation(uid) != stationUid)
                continue;

            EnsureComp<MediumComponent>(uid);
        }
    }
}
