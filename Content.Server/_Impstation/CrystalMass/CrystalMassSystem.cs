using System.Linq;
using System.Numerics;
using Content.Shared._EE.Supermatter.Components;
using Content.Shared._Impstation.CrystalMass;
using Content.Shared.Damage.Components;
using Content.Shared.Ghost;
// using Content.Shared.Interaction;
// using Content.Shared.Interaction.Components;
using Content.Shared.Item;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
// using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
// using Robust.Shared.Player;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
// using Robust.Shared.Utility;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server._Impstation.CrystalMass;

public sealed class CrystalMassSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    // [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<CrystalMassComponent, ComponentStartup>(OnStartup);
        // SubscribeLocalEvent<CrystalMassComponent, InteractHandEvent>(OnHandInteract);
        // SubscribeLocalEvent<CrystalMassComponent, InteractUsingEvent>(OnItemInteract);
        // SubscribeLocalEvent<CrystalMassComponent, StepTriggeredOffEvent>(OnStepTriggered);
    }

    private void OnStartup(EntityUid uid, CrystalMassComponent component, ComponentStartup args)
    {
        SetupCrystalMass(uid, component);
        ScheduleNextSpread(uid, component);
    }

    private void ScheduleNextSpread(EntityUid uid, CrystalMassComponent component)
    {
        var delay = _robustRandom.Next(1, 4);
        Timer.Spawn(delay * 1000, () =>
        {
            if (!Deleted(uid) && component.Spreading)
            {
                CrystalMassSpread(uid, component);
                ScheduleNextSpread(uid, component);
            }
        });
    }

    private void CrystalMassSpread(EntityUid uid, CrystalMassComponent component)
    {
        if (MetaData(uid).EntityPrototype?.ID == null)
        {
            component.Spreading = false;
            return;
        }
;
        var xform = Transform(uid);
        if (xform.GridUid == null)
            return;

        var gridUid = xform.GridUid.Value;

        if (!TryComp<MapGridComponent>(gridUid, out var mapGrid))
            return;

        var currentTile = _map.TileIndicesFor(gridUid, mapGrid, xform.Coordinates);
        var allOccupied = true;

        foreach (var avaialableDir in component.AvailableDirs)
        {
            var surroundingAnchoredEntities = _map.GetAnchoredEntitiesEnumerator(gridUid, mapGrid, currentTile.Offset(avaialableDir));
            var hasCrystalMass = false;

            while (surroundingAnchoredEntities.MoveNext(out var ent))
            {
                if (HasComp<CrystalMassComponent>(ent.Value))
                {
                    hasCrystalMass = true;
                    break;
                }
            }
            if (!hasCrystalMass)
            {
                allOccupied = false;
                break;
            }
        }

        if (allOccupied)
        {
            component.Spreading = false;
            return;
        }

        var dir = _robustRandom.Pick(component.AvailableDirs);
        var neighborTileCoords = Transform(uid).Coordinates.Offset(dir.ToVec());

        // TODO: Make floor sprite same as entity sprite, might or might not be better when loading areas with a lot of crystal mass
        _map.SetTile(gridUid, mapGrid, neighborTileCoords, new Tile(_tileDefManager["PlatingCrystalMass"].TileId, 0, (byte)_robustRandom.Next(0, component.SpriteVariants)));

        var newTile = _map.GetTileRef(gridUid, mapGrid, neighborTileCoords);

        // Needed cause crystal mass can't be found with Static Flag in GetEntitiesInTile for some reason
        var anchoredEntities = _map.GetAnchoredEntitiesEnumerator(gridUid, mapGrid, newTile.GridIndices);
        while (anchoredEntities.MoveNext(out var ent))
        {
            EntityManager.QueueDeleteEntity(ent);
        }

        // TODO: Find why there are entities department signs/directions & signs arn't getting destroyed
        foreach (var ent in _lookup.GetEntitiesInTile(newTile, LookupFlags.Dynamic | LookupFlags.Static | LookupFlags.Sundries))
        {
            // Prevents walls/windows from being deleted when next to a tile being spread to
            var entTile = _map.GetTileRef(gridUid, mapGrid, Transform(ent).Coordinates);
            if (entTile.GridIndices != newTile.GridIndices)
                continue;

            if (HasComp<SupermatterImmuneComponent>(ent) || HasComp<GodmodeComponent>(ent) || HasComp<GhostComponent>(ent))
                continue;

            if (HasComp<MobStateComponent>(ent) || HasComp<ItemComponent>(ent))
                _audio.PlayPvs(component.DustSound, ent, AudioParams.Default.WithVolume(-1f));

            EntityManager.QueueDeleteEntity(ent);
        }

        EntityUid spawnedEntity;
        if (_robustRandom.Prob(component.BulbChance))
        {
            spawnedEntity = Spawn("CrystalBulb", neighborTileCoords);
        }
        else
        {
            spawnedEntity = Spawn("CrystalMass", neighborTileCoords);
        }

        _audio.PlayPvs(component.CrackingCrystalSound, spawnedEntity, AudioParams.Default.WithVolume(3f));
    }

    private void SetupCrystalMass(EntityUid uid, CrystalMassComponent component)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        if (component.IsBulb)
            return;

        _appearance.SetData(uid, CrystalMassVisuals.Variant, _robustRandom.Next(1, component.SpriteVariants + 1), appearance);
    }
}
