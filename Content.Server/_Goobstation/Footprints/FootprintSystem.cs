// SPDX-FileCopyrightText: 2025 BombasterDS <deniskaporoshok@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

// imp cleanup on this whole thing to be more in line with wizden conventions -mq

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Server.Gravity;
using Content.Shared._Goobstation.Footprints;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Fluids.Components;
using Content.Shared.Standing;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Goobstation.Footprints;

public sealed class FootprintSystem : EntitySystem
{
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedPuddleSystem _puddle = default!;
    [Dependency] private readonly GravitySystem _gravity = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public static readonly FixedPoint2 MaxFootprintVolumeOnTile = 50;
    public static readonly EntProtoId FootprintPrototypeId = "Footprint";

    public const string FootprintOwnerSolution = "print";
    public const string FootprintSolution = "print";
    public const string PuddleSolution = "puddle";

    public override void Initialize()
    {
        SubscribeLocalEvent<FootprintComponent, FootprintCleanEvent>(OnFootprintClean);
        SubscribeLocalEvent<FootprintOwnerComponent, MoveEvent>(OnMove);
        SubscribeLocalEvent<PuddleComponent, MapInitEvent>(OnMapInit);
    }

    private void OnFootprintClean(Entity<FootprintComponent> ent, ref FootprintCleanEvent args)
    {
        ToPuddle(ent);
    }

    private void OnMove(Entity<FootprintOwnerComponent> ent, ref MoveEvent args)
    {
        if (_gravity.IsWeightless(ent.Owner) || !args.OldPosition.IsValid(EntityManager) || !args.NewPosition.IsValid(EntityManager))
            return;

        var oldPosition = _transform.ToMapCoordinates(args.OldPosition).Position;
        var newPosition = _transform.ToMapCoordinates(args.NewPosition).Position;

        ent.Comp.Distance += Vector2.Distance(newPosition, oldPosition);

        // foot sprites and body drag sprites are different sizes, so need different distances apart
        TryComp<StandingStateComponent>(ent, out var standingState);
        if (standingState is null)
            return;

        var standing = standingState.Standing;

        var requiredDistance = standing ? ent.Comp.FootDistance : ent.Comp.BodyDistance;

        if (ent.Comp.Distance < requiredDistance)
            return;

        ent.Comp.Distance -= requiredDistance;

        var transform = Transform(ent);

        if (transform.GridUid is null)
            return;

        if (!TryComp<MapGridComponent>(transform.GridUid.Value, out var gridComponent))
            return;

        EntityCoordinates coordinates = new(ent, standing ?
            ent.Comp.NextFootOffset :
            0, 0);

        ent.Comp.NextFootOffset = -ent.Comp.NextFootOffset;

        var tile = _map.CoordinatesToTile(transform.GridUid.Value, gridComponent, coordinates);

        if (TryPuddleInteraction(ent, (transform.GridUid.Value, gridComponent), tile, standing))
            return;

        // we only need rotation if we're putting down the footprint, since puddleinteraction deletes the footprint entity
        Angle rotation;

        if (!standing)
        {
            var oldLocalPosition = _map.WorldToLocal(transform.GridUid.Value, gridComponent, oldPosition);
            var newLocalPosition = _map.WorldToLocal(transform.GridUid.Value, gridComponent, newPosition);

            rotation = (newLocalPosition - oldLocalPosition).ToAngle();
        }
        else
            rotation = transform.LocalRotation;

        FootprintInteraction(ent, (transform.GridUid.Value, gridComponent), tile, coordinates, rotation, standing);
    }

    private bool TryPuddleInteraction(Entity<FootprintOwnerComponent> ent, Entity<MapGridComponent> grid, Vector2i tile, bool standing)
    {
        if (!TryGetAnchoredEntity<PuddleComponent>(grid, tile, out var puddle) ||
            !_solution.TryGetSolution(puddle.Value.Owner, PuddleSolution, out var puddleSolution, out _))
            return false;

        if (!_solution.EnsureSolutionEntity(ent.Owner, FootprintOwnerSolution, out _, out var solution,
            FixedPoint2.Max(ent.Comp.MaxFootVolume, ent.Comp.MaxBodyVolume)))
            return false;

        _solution.TryTransferSolution(puddleSolution.Value, solution.Value.Comp.Solution, GetFootprintVolume(ent, solution.Value));

        _solution.TryTransferSolution(solution.Value, puddleSolution.Value.Comp.Solution,
            FixedPoint2.Max(
            (standing ? ent.Comp.MaxFootVolume : ent.Comp.MaxBodyVolume) -
            solution.Value.Comp.Solution.Volume,
            0));

        _solution.UpdateChemicals(puddleSolution.Value, false);

        return true;
    }

    private void FootprintInteraction(Entity<FootprintOwnerComponent> ent, Entity<MapGridComponent> grid, Vector2i tile, EntityCoordinates coordinates, Angle rotation, bool standing)
    {
        if (!_solution.TryGetSolution(ent.Owner, FootprintOwnerSolution, out var solution, out _))
            return;

        var volume = standing ? GetFootprintVolume(ent, solution.Value) : GetBodyprintVolume(ent, solution.Value);

        if (volume < ent.Comp.MinFootprintVolume)
            return;

        if (!TryGetAnchoredEntity<FootprintComponent>(grid, tile, out var footprint))
        {
            var footprintEntity = SpawnAtPosition(FootprintPrototypeId, coordinates);

            footprint = (footprintEntity, Comp<FootprintComponent>(footprintEntity));
        }

        if (!_solution.EnsureSolutionEntity(
            footprint.Value.Owner, FootprintSolution,
            out _, out var footprintSolution,
            MaxFootprintVolumeOnTile))
            return;

        var color = solution.Value.Comp.Solution.GetColor(_prototype)
            .WithAlpha((float)volume /(float)(standing ?
            ent.Comp.MaxFootprintVolume :
            ent.Comp.MaxBodyprintVolume) / 2f);

        _solution.TryTransferSolution(footprintSolution.Value, solution.Value.Comp.Solution, volume);

        if (footprintSolution.Value.Comp.Solution.Volume >= MaxFootprintVolumeOnTile)
        {
            var footprintSolutionClone = footprintSolution.Value.Comp.Solution.Clone();

            Del(footprint);

            _puddle.TrySpillAt(coordinates, footprintSolutionClone, out _, false);

            return;
        }

        var gridCoords = _map.LocalToGrid(grid, grid, coordinates);

        var x = gridCoords.X / grid.Comp.TileSize;
        var y = gridCoords.Y / grid.Comp.TileSize;

        var halfTileSize = grid.Comp.TileSize / 2f;

        x -= MathF.Floor(x) + halfTileSize;
        y -= MathF.Floor(y) + halfTileSize;

        footprint.Value.Comp.Footprints.Add(new(
            new(x, y),
            rotation,
            color,
            standing ? ent.Comp.FootSprite : ent.Comp.BodySprite));

        Dirty(footprint.Value);

        if (!TryGetNetEntity(footprint, out var netFootprint))
            return;

        RaiseNetworkEvent(new FootprintChangedEvent(netFootprint.Value), Filter.Pvs(footprint.Value));
    }

    private void OnMapInit(Entity<PuddleComponent> ent, ref MapInitEvent args)
    {
        if (HasComp<FootprintComponent>(ent))
            return;

        var transform = Transform(ent);

        if (transform.GridUid is null)
            return;

        if (!TryComp<MapGridComponent>(transform.GridUid.Value, out var gridComponent))
            return;

        var tile = _map.CoordinatesToTile(transform.GridUid.Value, gridComponent, transform.Coordinates);

        if (!TryGetAnchoredEntity<FootprintComponent>((transform.GridUid.Value, gridComponent), tile, out var footprint))
            return;

        ToPuddle(footprint.Value, transform.Coordinates);
    }

    private void ToPuddle(EntityUid footprint, EntityCoordinates? coordinates = null)
    {
        coordinates ??= Transform(footprint).Coordinates;

        if (!_solution.TryGetSolution(footprint, FootprintSolution, out _, out var footprintSolution))
            return;

        footprintSolution = footprintSolution.Clone();

        Del(footprint);

        _puddle.TrySpillAt(coordinates.Value, footprintSolution, out _, false);
    }

    private static FixedPoint2 GetFootprintVolume(Entity<FootprintOwnerComponent> ent, Entity<SolutionComponent> solution)
    {
        return FixedPoint2.Min(solution.Comp.Solution.Volume,
            (ent.Comp.MaxFootprintVolume - ent.Comp.MinFootprintVolume) *
            (solution.Comp.Solution.Volume / ent.Comp.MaxFootVolume) +
            ent.Comp.MinFootprintVolume);
    }

    private static FixedPoint2 GetBodyprintVolume(Entity<FootprintOwnerComponent> ent, Entity<SolutionComponent> solution)
    {
        return FixedPoint2.Min(solution.Comp.Solution.Volume,
            (ent.Comp.MaxBodyprintVolume - ent.Comp.MinBodyprintVolume) *
            (solution.Comp.Solution.Volume / ent.Comp.MaxBodyVolume) +
            ent.Comp.MinBodyprintVolume);
    }

    private bool TryGetAnchoredEntity<T>(Entity<MapGridComponent> grid, Vector2i pos, [NotNullWhen(true)] out Entity<T>? entity) where T : IComponent
    {
        var anchoredEnumerator = _map.GetAnchoredEntitiesEnumerator(grid, grid, pos);
        var entityQuery = GetEntityQuery<T>();

        while (anchoredEnumerator.MoveNext(out var ent))
            if (entityQuery.TryComp(ent, out var comp))
            {
                entity = (ent.Value, comp);
                return true;
            }

        entity = null;
        return false;
    }
}
