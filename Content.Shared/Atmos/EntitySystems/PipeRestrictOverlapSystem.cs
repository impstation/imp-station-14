using System.Linq;
using Content.Shared._Funkystation.Atmos; // Imp
using Content.Shared._Funkystation.Atmos.Components; // Imp
using Content.Shared.Atmos.Components;
using Content.Shared.NodeContainer;
using Content.Shared.Popups; // Funky RPD
using Content.Shared.Construction.Components; // Funky RPD
using JetBrains.Annotations;
using Robust.Shared.Map.Components;
// Funky RPD, removed Content.Server.Atmos.Components, Content.Server.NodeContainer, Content.Server.NodeContainer.Nodes, Content.Server.Popups, Content.Shared.Atmos, Content.Shared.Construction.Components, & Robust.Server.GameObjects

namespace Content.Shared.Atmos.EntitySystems; // Funky RPD, moved file to shared

/// <summary>
/// This handles restricting pipe-based entities from overlapping outlets/inlets with other entities.
/// </summary>
public sealed class PipeRestrictOverlapSystem : EntitySystem
{
    // Funky RPD, moved to Shared
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    private readonly List<EntityUid> _anchoredEntities = new();
    private EntityQuery<NodeContainerComponent> _nodeContainerQuery;
    private EntityQuery<DeviceRestrictOverlapComponent> _deviceRestrictOverlapQuery; // Funky RPD

    public bool StrictDeviceStacking = true; // Funky RPD

    // Funky RPD Start
    public readonly record struct ProposedPipe(

        PipeDirection Direction,

        AtmosPipeLayer Layer,

        Angle Rotation = default);
    // Funky RPD End

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<PipeRestrictOverlapComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
        SubscribeLocalEvent<PipeRestrictOverlapComponent, AnchorAttemptEvent>(OnAnchorAttempt);
        // Funky - Added to handle device overlap
        SubscribeLocalEvent<DeviceRestrictOverlapComponent, AnchorStateChangedEvent>(OnDeviceAnchorStateChanged);
        SubscribeLocalEvent<DeviceRestrictOverlapComponent, AnchorAttemptEvent>(OnDeviceAnchorAttempt);

        _nodeContainerQuery = GetEntityQuery<NodeContainerComponent>();
        _deviceRestrictOverlapQuery = GetEntityQuery<DeviceRestrictOverlapComponent>();
    }

    private void OnAnchorStateChanged(Entity<PipeRestrictOverlapComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            return;

        if (HasComp<AnchorableComponent>(ent) && CheckOverlap(ent))
        {
            _popup.PopupEntity(Loc.GetString("pipe-restrict-overlap-popup-blocked", ("pipe", ent.Owner)), ent);
            _xform.Unanchor(ent, Transform(ent));
        }
    }

    private void OnAnchorAttempt(Entity<PipeRestrictOverlapComponent> ent, ref AnchorAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!_nodeContainerQuery.TryComp(ent, out var node))
            return;

        var xform = Transform(ent);
        if (CheckOverlap((ent, node, xform)))
        {
            _popup.PopupEntity(Loc.GetString("pipe-restrict-overlap-popup-blocked", ("pipe", ent.Owner)), ent, args.User);
            args.Cancel();
        }
    }

    /// <summary>
    /// Funky
    /// Checks if the device overlaps with any other devices.
    /// </summary>
    private void OnDeviceAnchorStateChanged(Entity<DeviceRestrictOverlapComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            return;

        if (HasComp<AnchorableComponent>(ent) && CheckDeviceOverlap(ent))
        {
            _popup.PopupEntity(Loc.GetString("device-restrict-overlap-popup-blocked", ("device", ent.Owner)), ent);
            _xform.Unanchor(ent, Transform(ent));
        }
    }

    private void OnDeviceAnchorAttempt(Entity<DeviceRestrictOverlapComponent> ent, ref AnchorAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (CheckDeviceOverlap(ent))
        {
            _popup.PopupEntity(Loc.GetString("device-restrict-overlap-popup-blocked", ("device", ent.Owner)), ent, args.User);
            args.Cancel();
        }
    }

    [PublicAPI]
    public bool CheckOverlap(EntityUid uid)
    {
        if (!_nodeContainerQuery.TryComp(uid, out var node))
            return false;

        return CheckOverlap((uid, node, Transform(uid)));
    }

    public bool CheckOverlap(Entity<NodeContainerComponent, TransformComponent> ent)
    {
        if (ent.Comp2.GridUid is not { } grid || !TryComp<MapGridComponent>(grid, out var gridComp))
            return false;

        var indices = _map.TileIndicesFor(grid, gridComp, ent.Comp2.Coordinates);
        _anchoredEntities.Clear();
        _map.GetAnchoredEntities((grid, gridComp), indices, _anchoredEntities);

        foreach (var otherEnt in _anchoredEntities)
        {
            // this should never actually happen but just for safety
            if (otherEnt == ent.Owner)
                continue;

            if (!_nodeContainerQuery.TryComp(otherEnt, out var otherComp))
                continue;

            if (PipeNodesOverlap(ent, (otherEnt, otherComp, Transform(otherEnt))))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Funky
    /// Checks if the device overlaps with any other devices.
    /// Returns true if it overlaps with another device.
    /// </summary>
    [PublicAPI]
    public bool CheckDeviceOverlap(EntityUid uid)
    {
        if (!_deviceRestrictOverlapQuery.HasComp(uid))
            return false;

        var xform = Transform(uid);
        if (xform.GridUid is not { } grid || !TryComp<MapGridComponent>(grid, out var gridComp))
            return false;

        var indices = _map.TileIndicesFor(grid, gridComp, xform.Coordinates);
        _anchoredEntities.Clear();
        _map.GetAnchoredEntities((grid, gridComp), indices, _anchoredEntities);

        foreach (var otherEnt in _anchoredEntities)
        {
            if (otherEnt == uid)
                continue;

            if (!_deviceRestrictOverlapQuery.HasComp(otherEnt))
                continue;

            if (StrictDeviceStacking)
                return true;
        }

        return false;
    }

    public bool PipeNodesOverlap(Entity<NodeContainerComponent, TransformComponent> ent, Entity<NodeContainerComponent, TransformComponent> other)
    {
        var entDirsAndLayers = GetAllDirectionsAndLayers(ent).ToList();
        var otherDirsAndLayers = GetAllDirectionsAndLayers(other).ToList();

        foreach (var (dir, layer) in entDirsAndLayers)
        {
            foreach (var (otherDir, otherLayer) in otherDirsAndLayers)
            {
                if ((dir & otherDir) != 0 && layer == otherLayer)
                    return true;
            }
        }

        return false;

        IEnumerable<(PipeDirection, AtmosPipeLayer)> GetAllDirectionsAndLayers(Entity<NodeContainerComponent, TransformComponent> pipe)
        {
            foreach (var node in pipe.Comp1.Nodes.Values)
            {
                if (node is IPipeNode pipeNode)
                    yield return (pipeNode.Direction.RotatePipeDirection(pipe.Comp2.LocalRotation), pipeNode.Layer); // Funky RPD, changed from pipeNode.OriginalPipeDirection.RotatePipeDirection(pipe.Comp2.LocalRotation), pipeNode.CurrentPipeLayer
            }
        }
    }

    // Funky RPD Start

    /// <summary>
    /// Checks if placing a new pipe with the given direction and layer on the specified tile would conflict
    /// with any existing anchored pipe on the same tile, same layer and overlapping direction.
    /// Returns the EntityUid of the first conflicting pipe found, or null if no conflict.
    /// </summary>
    public EntityUid? CheckIfWouldConflict(EntityUid gridUid,
        Vector2i tileIndices,
        ProposedPipe proposed,
        EntityUid? ignoreEntity = null)
    {
        if (!TryComp<MapGridComponent>(gridUid, out var gridComp))
            return null;

        // Pre-calculate the absolute direction of the proposed pipe
        var proposedDirAbs = proposed.Direction.RotatePipeDirection(proposed.Rotation);

        _anchoredEntities.Clear();
        _map.GetAnchoredEntities((gridUid, gridComp), tileIndices, _anchoredEntities);

        foreach (var otherEnt in _anchoredEntities)
        {
            if (otherEnt == ignoreEntity)
                continue;

            if (!_nodeContainerQuery.TryComp(otherEnt, out var otherNodeComp))
                continue;

            var otherXform = Transform(otherEnt);

            // Compare against the existing pipe's actual rotated nodes
            foreach (var (existingDir, existingLayer) in GetPipeNodeData((otherEnt, otherNodeComp, otherXform)))
            {
                // Conflict occurs if they share a layer AND any directional bit
                if (proposed.Layer == existingLayer && (proposedDirAbs & existingDir) != 0)
                    return otherEnt;
            }
        }

        return null;
    }

    private static IEnumerable<(PipeDirection RotatedDirection, AtmosPipeLayer Layer)> GetPipeNodeData(
        Entity<NodeContainerComponent, TransformComponent> pipe)
    {
        var rotation = pipe.Comp2.LocalRotation;

        foreach (var node in pipe.Comp1.Nodes.Values)
        {
            if (node is IPipeNode pipeNode)
                yield return (pipeNode.Direction.RotatePipeDirection(rotation), pipeNode.Layer);
        }
    }
    // Funky RPD End
}
