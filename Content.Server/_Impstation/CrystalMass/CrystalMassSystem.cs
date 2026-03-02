using Content.Server.Spreader;
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
using Content.Shared.Spreader;
using Robust.Shared.Audio.Systems;
// using Robust.Shared.Player;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

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

    private static readonly ProtoId<EdgeSpreaderPrototype> CrystalMassGroup = "CrystalMass";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<CrystalMassComponent, ComponentStartup>(SetupCrystalMass);
        SubscribeLocalEvent<CrystalMassComponent, SpreadNeighborsEvent>(OnCrystalMassSpread);
        // SubscribeLocalEvent<CrystalMassComponent, InteractHandEvent>(OnHandInteract);
        // SubscribeLocalEvent<CrystalMassComponent, InteractUsingEvent>(OnItemInteract);
        // SubscribeLocalEvent<CrystalMassComponent, StepTriggeredOffEvent>(OnStepTriggered);
    }

    private void OnCrystalMassSpread(EntityUid uid, CrystalMassComponent component, ref SpreadNeighborsEvent args)
    {
        if (args.NeighborFreeTiles.Count == 0)
        {
            RemCompDeferred<ActiveEdgeSpreaderComponent>(uid);
            return;
        }

        if (!_robustRandom.Prob(component.SpreadChance))
            return;

        if (MetaData(uid).EntityPrototype?.ID == null)
        {
            RemCompDeferred<ActiveEdgeSpreaderComponent>(uid);
            return;
        }
        foreach (var neighbor in args.NeighborFreeTiles)
        {
            foreach (var ent in _lookup.GetEntitiesInTile(neighbor.Tile, LookupFlags.Dynamic | LookupFlags.Static | LookupFlags.Sundries))
            {
                // Prevents walls/windows from being deleted when next to a tile being spread to
                var entTile = _map.GetTileRef(neighbor.Tile.GridUid, neighbor.Grid, Transform(ent).Coordinates);
                if (entTile.GridIndices != neighbor.Tile.GridIndices)
                    continue;

                if (HasComp<CrystalMassComponent>(ent) || HasComp<SupermatterImmuneComponent>(ent) || HasComp<GodmodeComponent>(ent) || HasComp<GhostComponent>(ent))
                    continue;

                if (HasComp<MobStateComponent>(ent) || HasComp<ItemComponent>(ent))
                    _audio.PlayPvs(component.DustSound, uid);

                EntityManager.QueueDeleteEntity(ent);
            }

            _map.SetTile(neighbor.Tile.GridUid, neighbor.Grid, neighbor.Tile.GridIndices, new Tile(_tileDefManager["PlatingCrystalMass"].TileId));
            var neighborUid = Spawn("CrystalMass", _map.GridTileToLocal(neighbor.Tile.GridUid, neighbor.Grid, neighbor.Tile.GridIndices));
            _audio.PlayPvs(component.CrackingCrystalSound, neighborUid);

            DebugTools.Assert(HasComp<EdgeSpreaderComponent>(neighborUid));
            DebugTools.Assert(HasComp<ActiveEdgeSpreaderComponent>(neighborUid));
            DebugTools.Assert(Comp<EdgeSpreaderComponent>(neighborUid).Id == CrystalMassGroup);

            args.Updates--;
            if (args.Updates <= 0)
                return;
        }
    }

    private void SetupCrystalMass(EntityUid uid, CrystalMassComponent component, ComponentStartup args)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        if (component.IsBulb)
            return;

        if (_robustRandom.Prob(component.BulbChance))
        {
            Spawn("CrystalBulb", Transform(uid).Coordinates);
            EntityManager.QueueDeleteEntity(uid);
        }
        else
        {
            _appearance.SetData(uid, CrystalMassVisuals.Variant, _robustRandom.Next(1, component.SpriteVariants + 1), appearance);
        }
    }
}
