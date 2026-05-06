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
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._Impstation.CrystalMass;

public sealed class CrystalMassSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    private static readonly ProtoId<EdgeSpreaderPrototype> CrystalMassGroup = "CrystalMass";
    private static readonly EntProtoId CrystalMassPrototype = "CrystalMass";
    private static readonly EntProtoId CrystalBulbPrototype = "CrystalBulb";
    private static readonly string CrystalMassPlating = "PlatingCrystalMass";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrystalMassComponent, ComponentStartup>(SetupCrystalMass);
        SubscribeLocalEvent<CrystalMassComponent, SpreadNeighborsEvent>(OnCrystalSpread);

        SubscribeLocalEvent<CrystalMassComponent, StepTriggerAttemptEvent>(OnStepTriggerAttempt);
        SubscribeLocalEvent<CrystalMassComponent, StepTriggeredOnEvent>(OnStepTriggered);
    }

    private void SetupCrystalMass(Entity<CrystalMassComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.IsBulb)
            return;

        if (!TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        _appearance.SetData(ent, CrystalMassVisuals.Variant, _robustRandom.Next(1, ent.Comp.SpriteVariants + 1), appearance);
    }

    private void OnCrystalSpread(Entity<CrystalMassComponent> ent, ref SpreadNeighborsEvent args)
    {
        var comp = ent.Comp;

        // Broken for some reason
        // if (args.Neighbor.Count == 0)
        // {
        //     RemCompDeferred<ActiveEdgeSpreaderComponent>(ent);
        //     return;
        // }

        if (!_robustRandom.Prob(comp.SpreadChance))
            return;

        var prototype = CrystalMassPrototype;
        if (_robustRandom.Prob(comp.BulbChance))
            prototype = CrystalBulbPrototype;

        var neighbor = _robustRandom.Pick(args.AllNeighbors);
        var neighborCoords = _map.GridTileToLocal(neighbor.GridUid, neighbor.Grid, neighbor.Position);

        var seed = _robustRandom.Next();
        var random = new Random(seed);
        var variant = _tile.PickVariant((ContentTileDefinition)_tileDefManager[CrystalMassPlating], random);

        _map.SetTile(neighbor.GridUid, neighbor.Grid, neighborCoords, new Tile(_tileDefManager[CrystalMassPlating].TileId, 0, variant));

        var neighborUid = Spawn(prototype, neighborCoords);
        DebugTools.Assert(HasComp<EdgeSpreaderComponent>(neighborUid));
        DebugTools.Assert(HasComp<ActiveEdgeSpreaderComponent>(neighborUid));
        DebugTools.Assert(Comp<EdgeSpreaderComponent>(neighborUid).Id == CrystalMassGroup);

        _audio.PlayPvs(ent.Comp.SpawningCrystalSound, ent, AudioParams.Default.WithVolume(20f));

        ClearNeighborTile((neighborUid, Comp<CrystalMassComponent>(neighborUid)), neighbor.GridUid, neighbor.Position);

        args.Updates--;
        if (args.Updates <= 0)
            return;
    }

    private void ClearNeighborTile(Entity<CrystalMassComponent> ent, EntityUid gridUid, Vector2i neighborGridIndices)
    {
        foreach (var target in _lookup.GetLocalEntitiesIntersecting(gridUid, neighborGridIndices, flags: LookupFlags.Uncontained))
        {
            if (target == ent.Owner)
                continue;

            if (HasComp<SupermatterImmuneComponent>(target)
                || HasComp<GodmodeComponent>(target)
                || HasComp<GhostComponent>(target))
                continue;

            if (HasComp<MobStateComponent>(target)
                || HasComp<ItemComponent>(target))
                _audio.PlayPvs(ent.Comp.DustSound, ent, AudioParams.Default.WithVolume(-2f));

            EntityManager.QueueDeleteEntity(target);
        }
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
