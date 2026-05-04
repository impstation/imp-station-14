using Content.Server.Spreader;
using Content.Shared._EE.Supermatter.Components;
using Content.Shared._Impstation.CrystalMass;
using Content.Shared.Damage.Components;
using Content.Shared.Ghost;
using Content.Shared.Item;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Spreader;
using Content.Shared.StepTrigger.Systems;
using System.Numerics;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._Impstation.CrystalMass;

public sealed class CrystalMassSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private static readonly ProtoId<EdgeSpreaderPrototype> CrystalMassGroup = "CrystalMass";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrystalMassComponent, ComponentStartup>(SetupCrystalMass);
        SubscribeLocalEvent<CrystalMassComponent, SpreadNeighborsEvent>(OnCrystalSpread);

        SubscribeLocalEvent<CrystalMassComponent, StepTriggerAttemptEvent>(OnStepTriggerAttempt);
        SubscribeLocalEvent<CrystalMassComponent, StepTriggeredOnEvent>(OnStepTriggered);
    }

    private void OnCrystalSpread(Entity<CrystalMassComponent> ent, ref SpreadNeighborsEvent args)
    {
        var uid = ent.Owner;

        if (!_robustRandom.Prob(ent.Comp.SpreadChance))
            return;

        var prototype = MetaData(uid).EntityPrototype?.ID;

        if (prototype == null)
        {
            RemCompDeferred<ActiveEdgeSpreaderComponent>(uid);
            return;
        }

        if (args.NeighborFreeTiles.Count == 0)
            return;

        var neighbor = _robustRandom.Pick(args.NeighborFreeTiles);
        var neighborUid = Spawn(prototype, _map.GridTileToLocal(neighbor.Tile.GridUid, neighbor.Grid, neighbor.Tile.GridIndices));
        DebugTools.Assert(HasComp<EdgeSpreaderComponent>(neighborUid));
        DebugTools.Assert(HasComp<ActiveEdgeSpreaderComponent>(neighborUid));
        DebugTools.Assert(Comp<EdgeSpreaderComponent>(neighborUid).Id == CrystalMassGroup);

        ClearTile(ent, neighbor);

        args.Updates--;
        if (args.Updates <= 0)
            return;
    }


    private void ClearTile(Entity<CrystalMassComponent> ent, (MapGridComponent Grid, TileRef Tile) neighbor)
    {
        var comp = ent.Comp;

        var worldPos = _transform.ToMapCoordinates(_map.GridTileToLocal(neighbor.Tile.GridUid, neighbor.Grid, neighbor.Tile.GridIndices)).Position;

        var gridRot = _transform.GetWorldRotation(neighbor.Tile.GridUid);
        var box = new Box2Rotated(Box2.CenteredAround(worldPos, Vector2.One), gridRot, worldPos);

        foreach (var target in _lookup.GetEntitiesIntersecting(Transform(ent).MapID, box, LookupFlags.Dynamic | LookupFlags.Static | LookupFlags.Sundries))
        {
            if (target == ent.Owner)
                continue;

            if (HasComp<SupermatterImmuneComponent>(target)
                || HasComp<GodmodeComponent>(target)
                || HasComp<GhostComponent>(target))
                continue;

            if (HasComp<MobStateComponent>(target)
                || HasComp<ItemComponent>(target))
                _audio.PlayPvs(comp.DustSound, ent, AudioParams.Default.WithVolume(-2f));

            EntityManager.QueueDeleteEntity(target);
        }
    }

    private void SetupCrystalMass(Entity<CrystalMassComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        if (ent.Comp.IsBulb)
            return;

        _appearance.SetData(ent, CrystalMassVisuals.Variant, _robustRandom.Next(1, ent.Comp.SpriteVariants + 1), appearance);
    }

    private void OnStepTriggered(Entity<CrystalMassComponent> ent, ref StepTriggeredOnEvent args)
    {
        if (HasComp<MobStateComponent>(args.Tripper)
            || HasComp<ItemComponent>(args.Tripper))
            _audio.PlayPvs(ent.Comp.DustSound, ent, AudioParams.Default.WithVolume(-2f));

        EntityManager.QueueDeleteEntity(args.Tripper);
    }

    private void OnStepTriggerAttempt(Entity<CrystalMassComponent> ent, ref StepTriggerAttemptEvent args)
    {
        if (HasComp<SupermatterImmuneComponent>(args.Tripper)
            || HasComp<GodmodeComponent>(args.Tripper)
            || HasComp<GhostComponent>(args.Tripper))
        {
            args.Cancelled = true;
            return;
        }

        args.Continue = true;
    }
}
