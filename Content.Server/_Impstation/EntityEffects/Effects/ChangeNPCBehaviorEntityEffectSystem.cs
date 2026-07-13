using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Server.Ghost.Roles.Components;
using Content.Server.StationEvents.Components;
using Content.Shared.EntityEffects;
using Content.Shared._Impstation.EntityEffects.Effects;
using Content.Shared.NPC;

namespace Content.Server._Impstation.EntityEffects.Effects;

/// <summary>
/// If target has HTNComponent, changes their root task, resets their targeting, removes their ghost role, and removes their ability to be targeted by the random sentience event.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class ChangeNPCBehaviorEntityEffectSystem : EntityEffectSystem<HTNComponent, ChangeNPCBehavior>
{
    [Dependency] private readonly HTNSystem _htn = default!;
    protected override void Effect(Entity<HTNComponent> entity, ref EntityEffectEvent<ChangeNPCBehavior> args)
    {
        // Changes the targets root task to whatever htnCompound is set.
        var htn = entity.Comp;
        htn.RootTask = new HTNCompoundTask { Task = args.Effect.rootTask };

        // Resets their targeting.
        htn.Blackboard.Remove<EntityUid>("Target");

        // Removes their ghost role and ability to be randomly made sentient.
        RemComp<GhostRoleComponent>(entity);
        RemComp<SentienceTargetComponent>(entity);
    }
}
