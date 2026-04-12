using Content.Server._Impstation.Slasher.Components;
using Content.Server.Chat.Managers;
using Content.Server.Ghost;
using Content.Shared.Actions;
using Content.Shared.Eye;
using Content.Shared.Mobs;
using Content.Shared._Impstation.Slasher.Components;
using Content.Shared.Mobs.Systems;
using Robust.Server.Player;

namespace Content.Server._Impstation.Slasher;

/// <summary>
/// Grants the Slasher role's actions on startup and removes any remaining action entities on shutdown.
/// This includes the live shroud toggle because the shroud system swaps that action at runtime.
/// Also prevents the Slasher from ghosting while their body is still in critical condition.
/// </summary>
public sealed class SlasherRoleSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;

    /// <summary>
    /// Subscribes role lifecycle, succumb handling, and ghost-attempt restrictions.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlasherRoleComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SlasherRoleComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SlasherRoleComponent, CritSuccumbEvent>(OnSuccumb);
        // Global hook — block the Slasher from ghosting while their body is still in crit.
        SubscribeLocalEvent<GhostAttemptHandleEvent>(OnGhostAttempt);
    }

    /// <summary>
    /// Forces dead state when a Slasher uses succumb in crit so they cannot get stuck in critical state.
    /// </summary>
    /// <param name="ent">Slasher role entity and component data.</param>
    /// <param name="args">Crit-succumb event data.</param>
    private void OnSuccumb(Entity<SlasherRoleComponent> ent, ref CritSuccumbEvent args)
    {
        if (!_mobState.IsCritical(ent.Owner))
            return;

        _mobState.ChangeMobState(ent.Owner, MobState.Dead);
        args.Handled = true;
    }

    /// <summary>
    /// Blocks the Slasher from ghosting while their body is in critical condition.
    /// Dead Slashers can ghost normally; only crit is gated.
    /// </summary>
    /// <param name="args">Ghost-attempt event data for the player's mind.</param>
    private void OnGhostAttempt(GhostAttemptHandleEvent args)
    {
        var body = args.Mind.OwnedEntity;
        if (body == null || !HasComp<SlasherRoleComponent>(body.Value))
            return;

        // Only block while crit — if the body is dead, ghosting is fine.
        if (!_mobState.IsCritical(body.Value))
            return;

        args.Handled = true;
        args.Result = false;

        // Notify the player via chat so they know why the attempt was rejected.
        if (_players.TryGetSessionById(args.Mind.UserId, out var session))
            _chatManager.DispatchServerMessage(session, Loc.GetString("slasher-ghost-blocked-crit"), suppressLog: true);
    }

    /// <summary>
    /// Grants configured role actions on startup and stores spawned action entities for cleanup.
    /// </summary>
    /// <param name="ent">Slasher role entity and component data.</param>
    /// <param name="args">Component startup event data.</param>
    private void OnStartup(Entity<SlasherRoleComponent> ent, ref ComponentStartup args)
    {
        _eye.RefreshVisibilityMask(ent.Owner);

        foreach (var action in ent.Comp.Actions)
        {
            EntityUid? actionEntity = null;
            if (_actions.AddAction(ent.Owner, ref actionEntity, action) && actionEntity is not null)
                ent.Comp.ActionEntities.Add(actionEntity.Value);
        }
    }

    /// <summary>
    /// Removes role-granted actions and any currently active shroud toggle action on shutdown.
    /// </summary>
    /// <param name="ent">Slasher role entity and component data.</param>
    /// <param name="args">Component shutdown event data.</param>
    private void OnShutdown(Entity<SlasherRoleComponent> ent, ref ComponentShutdown args)
    {
        // During ComponentShutdown this role component is still present, so RefreshVisibilityMask
        // would immediately re-apply the Slasher visibility bit.
        // Reset directly to the default eye mask instead.
        _eye.SetVisibilityMask(ent.Owner, EyeComponent.DefaultVisibilityMask);

        foreach (var action in ent.Comp.ActionEntities)
            _actions.RemoveAction(ent.Owner, action);
        ent.Comp.ActionEntities.Clear();

        if (!TryComp<SlasherShroudComponent>(ent.Owner, out var shroud))
            return;

        if (shroud.ActiveGainShroudActionEntity.HasValue)
            _actions.RemoveAction(ent.Owner, shroud.ActiveGainShroudActionEntity.Value);
        if (shroud.ActiveLoseShroudActionEntity.HasValue)
            _actions.RemoveAction(ent.Owner, shroud.ActiveLoseShroudActionEntity.Value);
    }
}
