using Content.Shared.Coordinates.Helpers;
using Content.Shared.DoAfter;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared._Impstation.Slasher;
using Content.Shared._Impstation.Slasher.Components;
using Content.Server._Impstation.Lighting;
using Robust.Shared.Map;

namespace Content.Server._Impstation.Slasher;

/// <summary>
/// Resolves the server side of the Slasher's dark-step action.
/// The teleport only starts if the targeted tile is valid and dark enough under the PVS-clamped luminance sample, then a do-after confirms the destination again before moving the user.
/// </summary>
public sealed class LightLevelTeleportSystem : SharedLightLevelTeleportSystem
{
    private const string DarkStepFailLoc = "slasher-dark-step-fail";
    private const string DarkStepStartLoc = "slasher-dark-step-start";
    private const string DarkStepSuccessLoc = "slasher-dark-step-success";

    [Dependency] private readonly LuminanceAtCoordinateSystem _luminance = default!;

    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    /// <summary>
    /// Starts dark-step channeling after validating the selected destination.
    /// </summary>
    /// <param name="ent">Action entity and its teleport configuration.</param>
    /// <param name="args">Action event data for the current use.</param>
    protected override void OnTeleport(Entity<LightLevelTeleportActionComponent> ent, ref SlasherDarkStepEvent args)
    {
        if (args.Handled)
            return;

        var targetMapCoords = _xform.ToMapCoordinates(args.Target);
        if (!TryGetDarkStepDestination(targetMapCoords, ent.Comp, out var destination))
        {
            _popup.PopupEntity(Loc.GetString(DarkStepFailLoc), args.Performer, args.Performer, PopupType.MediumCaution);
            return;
        }

        var riftUid = Spawn(ent.Comp.MarkerPrototype, destination);
        var doAfterEvent = new SlasherDarkStepDoAfterEvent(GetNetCoordinates(destination), GetNetEntity(riftUid));

        var doAfter = new DoAfterArgs(EntityManager,
            args.Performer,
            ent.Comp.ChannelTime,
            doAfterEvent,
            ent,
            used: args.Performer)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            BreakOnHandChange = true,
            NeedHand = false,
            BlockDuplicate = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
        {
            QueueDel(riftUid);
            return;
        }

        _popup.PopupEntity(Loc.GetString(DarkStepStartLoc), args.Performer, args.Performer);
        args.Handled = true;
    }

    /// <summary>
    /// Completes dark step by revalidating tile validity and teleporting the user if still valid.
    /// </summary>
    /// <param name="ent">Action entity and its teleport configuration.</param>
    /// <param name="args">Do-after completion data.</param>
    protected override void OnTeleportDoAfter(Entity<LightLevelTeleportActionComponent> ent, ref SlasherDarkStepDoAfterEvent args)
    {
        if (args.Cancelled)
        {
            CleanupDarkStepRift(args.Rift);
            return;
        }

        var destination = GetCoordinates(args.Destination);
        if (!TryGetTeleportCoordinates(_xform.ToMapCoordinates(destination), out var validatedDestination))
        {
            CleanupDarkStepRift(args.Rift);
            _popup.PopupEntity(Loc.GetString(DarkStepFailLoc), args.User, args.User, PopupType.MediumCaution);
            return;
        }

        CleanupDarkStepRift(args.Rift);
        _xform.SetCoordinates(args.User, validatedDestination);
        _popup.PopupEntity(Loc.GetString(DarkStepSuccessLoc), args.User, args.User);
    }

    /// <summary>
    /// Converts target map coordinates into teleport destination coordinates and applies the luminance gate.
    /// </summary>
    /// <param name="targetMapCoords">Requested destination in map space.</param>
    /// <param name="component">Teleport action configuration.</param>
    /// <param name="destination">Resolved destination coordinates when validation succeeds.</param>
    /// <returns>True when destination exists and satisfies the luminance check.</returns>
    private bool TryGetDarkStepDestination(MapCoordinates targetMapCoords,
        LightLevelTeleportActionComponent component,
        out EntityCoordinates destination)
    {
        if (!TryGetTeleportCoordinates(targetMapCoords, out destination))
            return false;

        var destinationMap = _xform.ToMapCoordinates(destination);
        return _luminance.Evaluate(destinationMap, component.AmbientDarkThreshold).MeetsThreshold;
    }

    /// <summary>
    /// Resolves and validates a teleport destination tile, returning snapped local coordinates on success.
    /// </summary>
    /// <param name="mapCoords">Target map coordinates selected by the user.</param>
    /// <param name="destination">Resolved snapped local destination coordinates when successful.</param>
    /// <returns>True when the destination tile is valid, non-space, and not impassable.</returns>
    private bool TryGetTeleportCoordinates(MapCoordinates mapCoords, out EntityCoordinates destination)
    {
        destination = EntityCoordinates.Invalid;

        if (!_mapManager.TryFindGridAt(mapCoords, out var gridUid, out var gridComp)
            || Transform(gridUid).MapUid == null)
        {
            return false;
        }

        var tile = _map.WorldToTile(gridUid, gridComp, mapCoords.Position);
        if (!_map.TryGetTileRef(gridUid, gridComp, tile, out var tileRef)
            || tileRef.Tile.IsEmpty
            || _turf.IsSpace(tileRef)
            || _turf.IsTileBlocked(tileRef, CollisionGroup.Impassable))
        {
            return false;
        }

        destination = _map.GridTileToLocal(gridUid, gridComp, tile).SnapToGrid();
        return true;
    }

    /// <summary>
    /// Deletes the temporary dark-step marker if it still exists.
    /// </summary>
    /// <param name="netRift">Network entity handle for the spawned marker.</param>
    private void CleanupDarkStepRift(NetEntity? netRift)
    {
        if (netRift == null)
            return;

        var riftUid = GetEntity(netRift.Value);
        if (Exists(riftUid))
            QueueDel(riftUid);
    }
}