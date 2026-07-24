using Content.Server.Administration.Managers;
using Content.Shared.Eye;
using Content.Shared.Ghost;
using Content.Shared._Impstation.Slasher.Components;
using Robust.Shared.Player;

namespace Content.Server._Impstation.Slasher;

/// <summary>
/// Adds the hidden effigy visibility layer to Slashers' eye masks.
/// </summary>
public sealed class SlasherVisionSystem : EntitySystem
{
    [Dependency] private readonly IAdminManager _adminManager = default!;

    /// <summary>
    /// Subscribes Slasher and global visibility-mask hooks for hidden-effigy layers.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlasherRoleComponent, GetVisMaskEvent>(OnGetVisMask);
        SubscribeLocalEvent<GetVisMaskEvent>(OnGlobalGetVisMask);
    }

    /// <summary>
    /// Adds the Slasher effigy visibility layer to the Slasher's view mask.
    /// </summary>
    /// <param name="ent">Entity and Slasher role component data.</param>
    /// <param name="args">Visibility mask event data for the viewer.</param>
    private static void OnGetVisMask(Entity<SlasherRoleComponent> ent, ref GetVisMaskEvent args)
    {
        args.VisibilityMask |= SlasherEffigyComponent.LayerMask;
    }

    /// <summary>
    /// Adds hidden-effigy visibility for ghosts and admins regardless of Slasher role.
    /// </summary>
    /// <param name="args">Visibility mask event data for the viewer.</param>
    private void OnGlobalGetVisMask(ref GetVisMaskEvent args)
    {
        // Let all ghosts (including aghost) observe hidden effigies.
        if (HasComp<GhostComponent>(args.Entity))
            args.VisibilityMask |= SlasherEffigyComponent.LayerMask;

        // Let current admins see hidden effigies for moderation/debugging.
        if (TryComp<ActorComponent>(args.Entity, out var actor) && _adminManager.IsAdmin(actor.PlayerSession))
            args.VisibilityMask |= SlasherEffigyComponent.LayerMask;
    }
}
