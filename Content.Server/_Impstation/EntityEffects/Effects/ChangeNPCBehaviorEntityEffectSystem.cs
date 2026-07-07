using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared.EntityEffects;
using Content.Shared._Impstation.EntityEffects.Effects;
using Content.Shared.NPC;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.EntityEffects.Effects;

/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class ChangeNPCBehaviorEntityEffectSystem : EntityEffectSystem<HTNComponent, ChangeNPCBehavior>
{
    [Dependency] private readonly HTNSystem _htn = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    protected override void Effect(Entity<HTNComponent> entity, ref EntityEffectEvent<ChangeNPCBehavior> args)
    {
        // Changes the targets root task to whatever htnCompound is set.
        EnsureComp<HTNComponent>(entity, out var htn);
        htn.RootTask = new HTNCompoundTask { Task = args.Effect.rootTask };

        // Resets their targeting.
        htn.Blackboard.Remove<EntityUid>("Target");
    }
}
