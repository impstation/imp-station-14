using Content.Shared._Impstation.Xenoarchaeology.Artifact.XAT.Components;
using Content.Shared.Mobs;
using Content.Shared.Stunnable;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT;

namespace Content.Shared._Impstation.Xenoarchaeology.Artifact.XAT.Systems;

/// <summary>
/// System for xeno artifact trigger that requires stunning of some mob near artifact.
/// </summary>
public sealed class XATStunSystem : BaseXATSystem<XATStunComponent>
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<XenoArtifactComponent> _xenoArtifactQuery;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        _xenoArtifactQuery = GetEntityQuery<XenoArtifactComponent>();

        SubscribeLocalEvent<StunnedComponent, StunnedEvent>(OnStun);
    }

    private void OnStun(Entity<StunnedComponent> stunned, ref StunnedEvent args)
    {
        var targetCoords = Transform(stunned).Coordinates;

        var query = EntityQueryEnumerator<XATStunComponent, XenoArtifactNodeComponent>();
        while (query.MoveNext(out var uid, out var comp, out var node))
        {
            if (node.Attached == null)
                continue;

            var artifact = _xenoArtifactQuery.Get(node.Attached.Value);

            if (!CanTrigger(artifact, (uid, node)))
                continue;

            var artifactCoords = Transform(artifact).Coordinates;
            if (_transform.InRange(targetCoords, artifactCoords, comp.Range))
                Trigger(artifact, (uid, comp, node));
        }
    }
}
