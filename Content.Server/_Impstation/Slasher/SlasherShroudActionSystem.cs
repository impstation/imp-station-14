using Content.Server.Actions;
using Content.Server.Body.Systems;
using Content.Server.Stealth;
using Content.Server._Impstation.Slasher.Components;
using Content.Shared.Body.Components;
using Content.Shared.Flash.Components;
using Content.Shared.CombatMode;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Shared.Stealth.Components;
using Content.Shared._Impstation.Slasher;
using Content.Shared._Impstation.Slasher.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Impstation.Slasher;

/// <summary>
/// Handles the Slasher's shroud actions.
/// Activating shroud applies stealth and bleed upkeep, while the paired action cleanly drops it.
/// </summary>
public sealed class SlasherShroudActionSystem : EntitySystem
{
    private static readonly EntProtoId ShroudActionProto = "ActionSlasherGainShroud";
    private static readonly EntProtoId UnshroudActionProto = "ActionSlasherLoseShroud";

    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StealthSystem _stealth = default!;

    /// <summary>
    /// Subscribes shroud action, combat-break, mob-state, and flash-break handlers.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlasherShroudActionComponent, SlasherGainShroudEvent>(OnGainShroud);
        SubscribeLocalEvent<SlasherUnshroudActionComponent, SlasherLoseShroudEvent>(OnLoseShroud);
        SubscribeLocalEvent<SlasherShroudComponent, ComponentStartup>(OnShroudStartup);
        SubscribeLocalEvent<SlasherShroudComponent, CombatModeChangedEvent>(OnCombatModeChanged);
        SubscribeLocalEvent<SlasherShroudComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<FlashedComponent, ComponentStartup>(OnFlashed);
    }

    /// <summary>
    /// Ensures shroud is not active when the component starts and user is already in combat mode.
    /// </summary>
    /// <param name="ent">Shroud entity and component data.</param>
    /// <param name="args">Component startup event data.</param>
    private void OnShroudStartup(Entity<SlasherShroudComponent> ent, ref ComponentStartup args)
    {
        TryBreakShroudForCombat(ent, ent.Comp, false);
    }

    /// <summary>
    /// Breaks shroud when combat mode is enabled.
    /// </summary>
    /// <param name="ent">Shroud entity and component data.</param>
    /// <param name="args">Combat mode change event data.</param>
    private void OnCombatModeChanged(Entity<SlasherShroudComponent> ent, ref CombatModeChangedEvent args)
    {
        if (!args.Enabled)
            return;

        TryBreakShroudForCombat(ent, ent.Comp);
    }

    /// <summary>
    /// Applies periodic bleed upkeep while users remain shrouded.
    /// </summary>
    /// <param name="frameTime">Frame delta time in seconds.</param>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SlasherShroudComponent, StealthComponent>();
        while (query.MoveNext(out var uid, out var shroud, out _))
        {
            if (_timing.CurTime < shroud.NextBleedTime)
                continue;

            var applied = ApplyBleedUpToCap(uid, shroud.BleedAmount, shroud.BleedCapRatio);
            shroud.ShroudBleedApplied += applied;
            shroud.NextBleedTime = _timing.CurTime.Add(shroud.BleedReapplyDelay);
        }
    }

    /// <summary>
    /// Activates shroud by enabling stealth, applying initial bleed, and swapping to the unshroud action.
    /// </summary>
    /// <param name="actionEnt">Gain-shroud action entity and configuration.</param>
    /// <param name="args">Instant action event data.</param>
    private void OnGainShroud(Entity<SlasherShroudActionComponent> actionEnt, ref SlasherGainShroudEvent args)
    {
        if (args.Handled)
            return;

        var user = args.Performer;
        if (HasComp<StealthComponent>(user))
            return;

        args.Handled = true;
        var shroud = EnsureComp<SlasherShroudComponent>(user);

        var applied = ApplyBleedUpToCap(user, actionEnt.Comp.BleedAmount, actionEnt.Comp.BleedCapRatio);

        EnsureComp<StealthComponent>(user);
        _stealth.SetMinVisibility(user, actionEnt.Comp.MinVisibility);
        _stealth.SetUseAltShader(user, true);
        _stealth.SetVisibility(user, actionEnt.Comp.MinVisibility);

        var stealthOnMove = EnsureComp<StealthOnMoveComponent>(user);
        stealthOnMove.MovementVisibilityRate = actionEnt.Comp.MovementVisibilityRate;

        shroud.BleedAmount = actionEnt.Comp.BleedAmount;
        shroud.BleedCapRatio = actionEnt.Comp.BleedCapRatio;
        shroud.ShroudBleedApplied = applied;
        shroud.BleedReapplyDelay = TimeSpan.FromSeconds(actionEnt.Comp.BleedReapplyDelay);
        shroud.NextBleedTime = _timing.CurTime + shroud.BleedReapplyDelay;
        ActivateShroud(user, shroud, args.Action);

        _popup.PopupEntity(Loc.GetString("slasher-shroud-on"), user, user);
    }

    /// <summary>
    /// Deactivates shroud when the user explicitly uses the unshroud action.
    /// </summary>
    /// <param name="actionEnt">Lose-shroud action entity and configuration.</param>
    /// <param name="args">Instant action event data.</param>
    private void OnLoseShroud(Entity<SlasherUnshroudActionComponent> actionEnt, ref SlasherLoseShroudEvent args)
    {
        if (args.Handled)
            return;

        var user = args.Performer;
        if (!TryComp<SlasherShroudComponent>(user, out var shroud) || !HasComp<StealthComponent>(user))
            return;

        args.Handled = true;

        DisableShroud(user, shroud);
        DeactivateShroud(user, shroud, args.Action);
        _popup.PopupEntity(Loc.GetString("slasher-shroud-off"), user, user);
        _popup.PopupEntity(Loc.GetString("slasher-shroud-off-others"), user, Filter.PvsExcept(user, entityManager: EntityManager), false);
    }

    /// <summary>
    /// Forces shroud removal when the user enters critical or dead state.
    /// </summary>
    /// <param name="uid">Entity receiving mob state update.</param>
    /// <param name="shroud">Current shroud runtime state.</param>
    /// <param name="args">Mob state change event data.</param>
    private void OnMobStateChanged(EntityUid uid, SlasherShroudComponent shroud, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Critical && args.NewMobState != MobState.Dead)
            return;

        if (!HasComp<StealthComponent>(uid))
            return;

        DisableShroud(uid, shroud);
        DeactivateShroud(uid, shroud, shroud.ActiveLoseShroudActionEntity);
    }

    /// <summary>
    /// Removes shroud when a flashed component is initialized while the user is hidden.
    /// </summary>
    /// <param name="ent">Flashed entity and component data.</param>
    /// <param name="args">Component startup event data.</param>
    private void OnFlashed(Entity<FlashedComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<SlasherShroudComponent>(ent, out var shroud) || !HasComp<StealthComponent>(ent))
            return;

        DisableShroud(ent, shroud);
        DeactivateShroud(ent, shroud, shroud.ActiveLoseShroudActionEntity);
        _popup.PopupEntity(Loc.GetString("slasher-shroud-broken-by-flash"), ent, ent, PopupType.LargeCaution);
        _popup.PopupEntity(Loc.GetString("slasher-shroud-broken-by-flash-others"), ent, Filter.PvsExcept(ent, entityManager: EntityManager), false, PopupType.LargeCaution);
    }

    /// <summary>
    /// Swaps gain-shroud action for lose-shroud action when shroud becomes active.
    /// </summary>
    /// <param name="user">User whose actions are being swapped.</param>
    /// <param name="shroud">Shroud runtime state to update.</param>
    /// <param name="gainShroudAction">Optional gain-shroud action entity to remove.</param>
    private void ActivateShroud(EntityUid user, SlasherShroudComponent shroud, EntityUid? gainShroudAction)
    {
        var toRemove = gainShroudAction ?? shroud.ActiveGainShroudActionEntity;
        _actions.RemoveAction(user, toRemove);

        EntityUid? loseShroudEntity = null;
        _actions.AddAction(user, ref loseShroudEntity, UnshroudActionProto);

        shroud.ActiveGainShroudActionEntity = null;
        shroud.ActiveLoseShroudActionEntity = loseShroudEntity;
    }

    /// <summary>
    /// Swaps lose-shroud action back to gain-shroud action when shroud ends.
    /// </summary>
    /// <param name="user">User whose actions are being swapped.</param>
    /// <param name="shroud">Shroud runtime state to update.</param>
    /// <param name="loseShroudAction">Optional lose-shroud action entity to remove.</param>
    private void DeactivateShroud(EntityUid user, SlasherShroudComponent shroud, EntityUid? loseShroudAction)
    {
        var toRemove = loseShroudAction ?? shroud.ActiveLoseShroudActionEntity;
        _actions.RemoveAction(user, toRemove);

        EntityUid? gainShroudEntity = null;
        _actions.AddAction(user, ref gainShroudEntity, ShroudActionProto);

        shroud.ActiveLoseShroudActionEntity = null;
        shroud.ActiveGainShroudActionEntity = gainShroudEntity;
    }

    /// <summary>
    /// Applies bleed without exceeding the configured fraction of the user's maximum bleed cap.
    /// </summary>
    /// <param name="user">User receiving bleed.</param>
    /// <param name="gain">Requested bleed amount to add.</param>
    /// <param name="bleedCapRatio">Fraction of max bleed that shroud can maintain.</param>
    /// <returns>Actual bleed amount applied.</returns>
    private float ApplyBleedUpToCap(EntityUid user, float gain, float bleedCapRatio)
    {
        if (!TryComp<BloodstreamComponent>(user, out var bloodstream))
            return 0f;

        var bleedCap = bloodstream.MaxBleedAmount * bleedCapRatio;
        var canAdd = Math.Max(0f, bleedCap - bloodstream.BleedAmount);
        var toAdd = Math.Min(gain, canAdd);

        if (toAdd <= 0f)
            return 0f;

        if (!_bloodstream.TryModifyBleedAmount((user, bloodstream), toAdd))
            return 0f;

        return toAdd;
    }

    /// <summary>
    /// Clears stealth state and removes bleed attributed to the current shroud instance.
    /// </summary>
    /// <param name="user">User to unshroud.</param>
    /// <param name="shroud">Shroud runtime state to clear.</param>
    private void DisableShroud(EntityUid user, SlasherShroudComponent shroud)
    {
        if (shroud.ShroudBleedApplied > 0f)
        {
            _bloodstream.TryModifyBleedAmount(user, -shroud.ShroudBleedApplied);
            shroud.ShroudBleedApplied = 0f;
        }

        RemComp<StealthComponent>(user);
        RemComp<StealthOnMoveComponent>(user);
    }

    /// <summary>
    /// Breaks shroud when user is in combat mode.
    /// </summary>
    /// <param name="user">User whose shroud may be broken.</param>
    /// <param name="shroud">Shroud runtime state.</param>
    /// <param name="showPopups">Whether to show unshroud popups.</param>
    private void TryBreakShroudForCombat(EntityUid user, SlasherShroudComponent shroud, bool showPopups = true)
    {
        if (!HasComp<StealthComponent>(user))
            return;

        if (!TryComp<CombatModeComponent>(user, out var combat) || !combat.IsInCombatMode)
            return;

        DisableShroud(user, shroud);
        DeactivateShroud(user, shroud, shroud.ActiveLoseShroudActionEntity);

        if (showPopups)
        {
            _popup.PopupEntity(Loc.GetString("slasher-shroud-off"), user, user);
            _popup.PopupEntity(Loc.GetString("slasher-shroud-off-others"), user, Filter.PvsExcept(user, entityManager: EntityManager), false);
        }
    }
}