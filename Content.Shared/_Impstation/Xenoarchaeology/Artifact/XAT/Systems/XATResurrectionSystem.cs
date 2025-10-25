using Content.Shared._Impstation.Xenoarchaeology.Artifact.XAT.Components;
using Content.Shared.Mobs;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT;

namespace Content.Shared._Impstation.Xenoarchaeology.Artifact.XAT.Systems;

/// <summary>
/// System for xeno artifact trigger that requires resurrection of some mob near artifact.
/// </summary>
public sealed class XATResurrectionSystem : BaseXATSystem<XATResurrectionComponent>
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<XenoArtifactComponent> _xenoArtifactQuery;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        _xenoArtifactQuery = GetEntityQuery<XenoArtifactComponent>();

        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (args.OldMobState != MobState.Dead || args.NewMobState == MobState.Dead)
            return;

        var targetCoords = Transform(args.Target).Coordinates;

        var query = EntityQueryEnumerator<XATResurrectionComponent, XenoArtifactNodeComponent>();
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
