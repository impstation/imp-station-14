using Content.Server._Impstation.Traitor.Components;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Roles;
using Content.Shared.Mind.Components;

namespace Content.Server._Impstation.Traitor.Systems;

/// <summary>
/// Makes entities with <see cref="RandomAntagChanceComponent"/> the defined antag at a set random chance.
/// </summary>
public sealed class RandomAntagChanceSystem : EntitySystem
{
    [Dependency] private readonly RoleSystem _role = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomAntagChanceComponent, TakeGhostRoleEvent>(OnMindAdded);
    }

    private void OnMindAdded(EntityUid uid, RandomAntagChanceComponent comp, TakeGhostRoleEvent args)
    {
        var random = new Random();
        if (random.NextDouble() > comp.Chance)
            return;

        _role.MindAddRole(args.Mind, comp.Profile, mind: args.Mind);
    }
}
