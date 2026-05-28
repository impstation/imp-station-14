using Content.Shared._Impstation.Xenoarchaeology.Artifact.XAT.Components;
using Content.Shared.Dice;
using Content.Shared.Mobs;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT;

namespace Content.Shared._Impstation.Xenoarchaeology.Artifact.XAT.Systems;

/// <summary>
/// System for xeno artifact trigger that activates when a die is rolled with a certain value nearby.
/// </summary>
public sealed class XATDiceRolledSystem : BaseXATSystem<XATDiceRolledComponent>
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<XenoArtifactComponent> _xenoArtifactQuery;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        _xenoArtifactQuery = GetEntityQuery<XenoArtifactComponent>();

        SubscribeLocalEvent<DiceRolledEvent>(OnDiceRolled);
    }

    private void OnDiceRolled(ref DiceRolledEvent args)
    {

        // Not going to assume that there will never be an entity with DiceComp but not with TransformComp.
        if (!TryComp<TransformComponent>(args.Dice.Owner, out var diceTransform))
            return;

        var targetCoords = diceTransform.Coordinates;

        var query = EntityQueryEnumerator<XATDiceRolledComponent, XenoArtifactNodeComponent>();
        while (query.MoveNext(out var uid, out var comp, out var node))
        {
            if (node.Attached == null)
                continue;

            // Dice must have at list MinSides sides
            if (comp.MinSides > args.Sides)
                continue;

            var artifact = _xenoArtifactQuery.Get(node.Attached.Value);

            if (!CanTrigger(artifact, (uid, node)))
                continue;

            var artifactCoords = Transform(artifact).Coordinates;
            if (!_transform.InRange(targetCoords, artifactCoords, comp.Range))
                continue;

            var adjustedTarget = ((((float)comp.TargetSide - 1) / comp.MinSides) * args.Sides) + 1;

            switch (comp.Mode)
            {
                case "High":
                {
                    if (adjustedTarget <= args.Roll)
                        Trigger(artifact, (uid, comp, node));
                    break;
                }
                case "Low":
                {
                    if (adjustedTarget >= args.Roll)
                        Trigger(artifact, (uid, comp, node));
                    break;
                }
                case "Target":
                {
                    if (args.Roll == comp.TargetSide)
                        Trigger(artifact, (uid, comp, node));
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
