using Content.Shared.Xenoarchaeology.Artifact.XAE.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Xenoarchaeology.Artifact.XAE;

/// <summary>
/// System for applying component-registry when artifact effect is activated.
/// </summary>
public sealed class XAEApplyComponentsSystem : BaseXAESystem<XAEApplyComponentsComponent>
{
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAEApplyComponentsComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var artifact = args.Artifact;

        foreach (var registry in ent.Comp.Components)
        {
            var componentType = registry.Value.Component.GetType();

            //#IMP remove component when a natural artifact node is no longer current and EffectActiveOnlyWhileNodeIsCurrent = true on nodecomp
            if (args.Deactivate && HasComp(artifact, componentType))
            {
                RemComp(artifact, componentType);
                continue;
            }

            if (!ent.Comp.ApplyIfAlreadyHave && HasComp(artifact, componentType))
            {
                continue;
            }

            if (ent.Comp.RefreshOnReactivate)
            {
                RemComp(artifact, componentType);
            }

            var clone = EntityManager.ComponentFactory.GetComponent(registry.Value);
            AddComp(artifact, clone);
        }
    }
}
