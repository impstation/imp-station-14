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
/// Manages Slasher role lifecycle, actions, and ghost blocking.
/// </summary>
public sealed class SlasherRoleSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly SlasherDeathTeleportSystem _deathTeleport = default!;

    /// <summary>
    /// Subscribe to role events.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlasherRoleComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SlasherRoleComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SlasherRoleComponent, CritSuccumbEvent>(OnSuccumb);
        // Global hook: block Slasher ghosting while the body is still in crit.
        SubscribeLocalEvent<GhostAttemptHandleEvent>(OnGhostAttempt);
    }

    /// <summary>
    /// Force dead state on succumb.
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
    /// Block ghosting while crit. Dead Slashers can still ghost.
    /// </summary>
    /// <param name="args">Ghost-attempt event data for the player's mind.</param>
    private void OnGhostAttempt(GhostAttemptHandleEvent args)
    {
        var body = args.Mind.OwnedEntity;
        if (body == null || !HasComp<SlasherRoleComponent>(body.Value))
            return;

        // Only block crit. Dead Slashers can ghost.
        if (!_mobState.IsCritical(body.Value))
            return;

        args.Handled = true;
        args.Result = false;

        // Tell the player why ghosting was blocked.
        if (_players.TryGetSessionById(args.Mind.UserId, out var session))
            _chatManager.DispatchServerMessage(session, Loc.GetString("slasher-ghost-blocked-crit"), suppressLog: true);
    }

    /// <summary>
    /// Grant role actions on startup.
    /// </summary>
    /// <param name="ent">Slasher role entity and component data.</param>
    /// <param name="args">Component startup event data.</param>
    private void OnStartup(Entity<SlasherRoleComponent> ent, ref ComponentStartup args)
    {
        _eye.RefreshVisibilityMask(ent.Owner);

        // Preload and validate the shared death maze while the role is granted, not at death time.
        _deathTeleport.TryGetDeathMazeSpawn(out _, DeathMazeSpawnSelection.Any);

        foreach (var action in ent.Comp.Actions)
        {
            EntityUid? actionEntity = null;
            if (_actions.AddAction(ent.Owner, ref actionEntity, action) && actionEntity is not null)
                ent.Comp.ActionEntities.Add(actionEntity.Value);
        }
    }

    /// <summary>
    /// Clean up role actions on shutdown.
    /// </summary>
    /// <param name="ent">Slasher role entity and component data.</param>
    /// <param name="args">Component shutdown event data.</param>
    private void OnShutdown(Entity<SlasherRoleComponent> ent, ref ComponentShutdown args)
    {
        // RefreshVisibilityMask would re-apply the Slasher visibility bit here,
        // so reset straight to the default eye mask instead.
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
