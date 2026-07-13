using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared.EntityEffects;
using Content.Shared._Impstation.EntityEffects.Effects;
using Content.Shared.NPC;

namespace Content.Server._Impstation.EntityEffects.Effects;

/// <summary>
/// If target has HTNComponent, changes their root task and resets their targeting.
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
    }
}
