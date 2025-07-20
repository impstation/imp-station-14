using System.Numerics;
using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Throwing;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

public sealed class ThrowArtifactSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ThrowArtifactComponent, ArtifactActivatedEvent>(OnActivated);
    }

    private void OnActivated(Entity<ThrowArtifactComponent> ent, ref ArtifactActivatedEvent args)
    {
        var xform = Transform(ent);
        if (TryComp<MapGridComponent>(xform.GridUid, out var grid))
        {
            var tiles = _mapSystem.GetTilesIntersecting(
                xform.GridUid.Value,
                grid,
                Box2.CenteredAround(_transform.GetWorldPosition(xform), new Vector2(ent.Comp.Range * 2, ent.Comp.Range)));

            foreach (var tile in tiles)
            {
                if (!_random.Prob(ent.Comp.TilePryChance))
                    continue;

                _tile.PryTile(tile);
            }
        }

        var lookup = _lookup.GetEntitiesInRange(ent, ent.Comp.Range, LookupFlags.Dynamic | LookupFlags.Sundries);
        var physQuery = GetEntityQuery<PhysicsComponent>();
        foreach (var target in lookup)
        {
            if (physQuery.TryGetComponent(target, out var phys)
                && (phys.CollisionMask & (int) CollisionGroup.GhostImpassable) != 0)
                continue;

            var tempXform = Transform(target);

            var foo = _transform.GetMapCoordinates(target, xform: tempXform).Position - _transform.GetMapCoordinates(ent, xform: xform).Position;
            _throwing.TryThrow(target, foo*2, ent.Comp.ThrowStrength, ent, 0);
        }
    }
}