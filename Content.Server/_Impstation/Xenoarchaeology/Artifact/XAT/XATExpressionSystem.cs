using Content.Server.Chat.Systems;
using Content.Server.Xenoarchaeology.Artifact.XAT.Components;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT;

namespace Content.Server.Xenoarchaeology.Artifact.XAT;

/// <summary>
/// System for checking if emote-triggered xenoartifact should be triggered.
/// </summary>
public sealed class XATExpressionSystem : BaseQueryUpdateXATSystem<XATExpressionComponent>
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TransformComponent, EntityEmotedEvent>(OnEmote);
    }


    protected override void UpdateXAT(Entity<XenoArtifactComponent> artifact, Entity<XATExpressionComponent, XenoArtifactNodeComponent> node, float frameTime)
    {
        return;
    }

    private void OnEmote(EntityUid emoter, TransformComponent component, EntityEmotedEvent args)
    {
        if (!HasComp<TransformComponent>(args.Source))
            return;

        var emoterCoordinates = Transform(args.Source).Coordinates;

        var query = EntityQueryEnumerator<XATExpressionComponent, XenoArtifactNodeComponent>();
        while (query.MoveNext(out var uid, out var comp, out var node))
        {
            if (node.Attached == null)
                continue;

            var artifact = _xenoArtifactQuery.Get(node.Attached.Value);

            if (!CanTrigger(artifact, (uid, node)))
                continue;

            var artifactCoordinates = Transform(artifact).Coordinates;
            if (_transform.InRange(emoterCoordinates, artifactCoordinates, comp.Range))
                Trigger(artifact, (uid, comp, node));
        }
    }
}
