using Content.Server.Ghost.Roles.Components;
using Content.Server.Mind;

namespace Content.Server._Impstation.Antag;

/// <summary>
/// System to a player a set of objectives upon taking a over ghost role entity
/// </summary>
public sealed partial class GrantObjectivesSystem : EntitySystem
{
    [Dependency] private readonly MindSystem _mind = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GrantObjectivesComponent, TakeGhostRoleEvent>(GrantObjectives);
    }

    /// <summary>
    /// Grant the player objectives upon taking control
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="args"></param>
    private void GrantObjectives(Entity<GrantObjectivesComponent> ent, ref TakeGhostRoleEvent args)
    {
        if (!_mind.TryGetMind(args.Player, out var mindId, out var mind))
            return;
        foreach (var objective in ent.Comp.Objectives)
        {
            _mind.TryAddObjective(mindId, mind, objective);
        }

    }
}
