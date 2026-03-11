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
using Content.Shared.StepTrigger.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
// using Robust.Shared.Player;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;
// using Robust.Shared.Utility;

namespace Content.Server._Impstation.CrystalMass;

public sealed class CrystalMassSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    // [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrystalMassComponent, ComponentStartup>(OnStartup);
        // SubscribeLocalEvent<CrystalMassComponent, InteractHandEvent>(OnHandInteract); TODO: Add little touch interactions like SupermatterSystem.cs
        // SubscribeLocalEvent<CrystalMassComponent, InteractUsingEvent>(OnItemInteract);
        SubscribeLocalEvent<CrystalMassComponent, StepTriggerAttemptEvent>(OnStepTriggerAttempt);
        SubscribeLocalEvent<CrystalMassComponent, StepTriggeredOnEvent>(OnStepTriggered);
    }

    private void OnStartup(Entity<CrystalMassComponent> ent, ref ComponentStartup args)
    {
        SetupCrystalMass(ent);
        // Actual ss13 times would be 0, 3 but they have a update delay, aksi this feels more right imo
        // 0 delay multiple times will cause it to spike out suddenly
        var delay = _random.Next(1, 5);
        ent.Comp.NextSpread = _timing.CurTime + TimeSpan.FromSeconds(delay);

        // TODO: REMOVE TIMER.SPAWN
        // Slightly longer than one tick
        Timer.Spawn(TimeSpan.FromMilliseconds(17), () =>
        {
            ClearTileForCrystal(ent);
        });
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var toSpread = new List<Entity<CrystalMassComponent>>();
        var query = EntityQueryEnumerator<CrystalMassComponent>();

        while (query.MoveNext(out var ent, out var crystalMassComponent))
        {
            if (!crystalMassComponent.Spreading)
                continue;
            if (crystalMassComponent.NextSpread > curTime)
                continue;

            toSpread.Add(new Entity<CrystalMassComponent>(ent, crystalMassComponent));
        }

        foreach (var ent in toSpread)
        {
            CrystalMassSpread(ent);

            var delay = _random.Next(1, 5);
            ent.Comp.NextSpread = _timing.CurTime + TimeSpan.FromSeconds(delay);
        }
    }

    private void CrystalMassSpread(Entity<CrystalMassComponent> ent)
    {
        if (MetaData(ent).EntityPrototype?.ID == null)
        {
            ent.Comp.Spreading = false;
            return;
        }

        // Figure out a way to not repeat logic for grid/world cords in both CrystalMassSpread & ClearTileForCrystal
        var xform = Transform(ent);
        if (xform.GridUid == null)
            return;

        var gridUid = xform.GridUid.Value;

        if (!TryComp<MapGridComponent>(gridUid, out var mapGrid))
            return;

        var currentTile = _map.TileIndicesFor(gridUid, mapGrid, xform.Coordinates);

        var allOccupied = true;

        foreach (var availableDir in ent.Comp.AvailableDirs)
        {
            var surroundingAnchoredEntities = _map.GetAnchoredEntitiesEnumerator(gridUid, mapGrid, currentTile.Offset(availableDir));
            var hasCrystalMass = false;

            while (surroundingAnchoredEntities.MoveNext(out var anchoredEnt))
            {
                if (HasComp<CrystalMassComponent>(anchoredEnt.Value))
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
            ent.Comp.Spreading = false;
            return;
        }

        var dir = _random.Pick(ent.Comp.AvailableDirs);
        var neighborTileCoords = Transform(ent).Coordinates.Offset(dir.ToVec());

        // TODO: Check if spreadersystem stuff is deleted
        // TODO: Make floor sprite same as entity sprite, might or might not be better when loading areas with a lot of crystal mass
        // TODO: Add limiter so it dosen't spread in space infinitly
        _map.SetTile(gridUid, mapGrid, neighborTileCoords, new Tile(_tileDefManager["PlatingCrystalMass"].TileId, 0, (byte)_random.Next(0, ent.Comp.SpriteVariants)));

        // Engine lighting breaks when there are too many lights, limited to only bulb when it should all glow
        if (_random.Prob(ent.Comp.BulbChance))
        {
            Spawn("CrystalBulb", neighborTileCoords);
        }
        else
        {
            Spawn("CrystalMass", neighborTileCoords);
        }

        _audio.PlayPvs(ent.Comp.CrackingCrystalSound, ent, AudioParams.Default.WithVolume(20f));
    }

    public void ClearTileForCrystal(Entity<CrystalMassComponent> ent)
    {
        if (Deleted(ent))
            return;

        var xform = Transform(ent);
        if (xform.GridUid == null)
            return;

        var gridUid = xform.GridUid.Value;

        if (!TryComp<MapGridComponent>(gridUid, out var mapGrid))
            return;

        var currentTile = _map.TileIndicesFor(gridUid, mapGrid, xform.Coordinates);

        // Needed cause crystal mass can't be found with Static Flag in GetEntitiesInTile for some reason
        var anchoredEntities = _map.GetAnchoredEntitiesEnumerator(gridUid, mapGrid, currentTile);
        while (anchoredEntities.MoveNext(out var anchoredEnt))
        {
            if (anchoredEnt.Value == ent.Owner)
                continue;
            EntityManager.QueueDeleteEntity(anchoredEnt.Value);
        }

        // TODO: Find why there are entities department signs/directions & signs that arn't getting destroyed
        foreach (var targetEnt in _lookup.GetEntitiesInTile(_map.GetTileRef(gridUid, mapGrid, currentTile), LookupFlags.Dynamic | LookupFlags.Static | LookupFlags.Sundries))
        {
            if (targetEnt == ent.Owner)
                continue;

            // Prevents walls/windows from being deleted when next to a tile being spread to
            var entTile = _map.GetTileRef(gridUid, mapGrid, Transform(targetEnt).Coordinates);
            if (entTile.GridIndices != currentTile)
                continue;

            if (HasComp<SupermatterImmuneComponent>(targetEnt) || HasComp<GodmodeComponent>(targetEnt) || HasComp<GhostComponent>(targetEnt))
                continue;

            if (HasComp<MobStateComponent>(targetEnt) || HasComp<ItemComponent>(targetEnt))
                _audio.PlayPvs(ent.Comp.DustSound, ent, AudioParams.Default.WithVolume(-2f));

            EntityManager.QueueDeleteEntity(targetEnt);
        }
    }

    private void SetupCrystalMass(Entity<CrystalMassComponent> ent)
    {
        if (!TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        if (ent.Comp.IsBulb)
            return;

        _appearance.SetData(ent, CrystalMassVisuals.Variant, _random.Next(1, ent.Comp.SpriteVariants + 1), appearance);
    }

    private void OnStepTriggered(Entity<CrystalMassComponent> ent, ref StepTriggeredOnEvent args)
    {
        if (HasComp<MobStateComponent>(args.Tripper) || HasComp<ItemComponent>(args.Tripper))
            _audio.PlayPvs(ent.Comp.DustSound, ent, AudioParams.Default.WithVolume(-2f));

        EntityManager.QueueDeleteEntity(args.Tripper);
    }

    private void OnStepTriggerAttempt(Entity<CrystalMassComponent> ent, ref StepTriggerAttemptEvent args)
    {
        if (HasComp<SupermatterImmuneComponent>(args.Tripper) || HasComp<GodmodeComponent>(args.Tripper) || HasComp<GhostComponent>(args.Tripper))
        {
            args.Cancelled = true;
            return;
        }

        args.Continue = true;
    }
}
