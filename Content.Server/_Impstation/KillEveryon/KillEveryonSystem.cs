using Content.Server.Antag;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Mind;
using Content.Shared.Mind;
using Content.Shared.Players;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;

namespace Content.Server._Impstation.KillEveryon;


public sealed partial class KillEveryonSystem : EntitySystem
{
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KillEveryonComponent, TakeGhostRoleEvent>(KillEveryon);
    }

    private void KillEveryon(Entity<KillEveryonComponent> ent, ref TakeGhostRoleEvent args)
    {
        if (!_mind.TryGetMind(args.Player, out var mindId, out var mind))
            return;
        _mind.TryAddObjective(mindId, mind, ent.Comp.Obj);
    }
}
