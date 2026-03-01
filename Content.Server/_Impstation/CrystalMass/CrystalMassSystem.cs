using Content.Server._Impstation.CrystalMass;
using Content.Server.Spreader;
using Content.Shared._Impstation.CrystalMass;
using Content.Shared.Spreader;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._Impstation.CrystalMass;

public sealed class CrystalMassSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    private static readonly ProtoId<EdgeSpreaderPrototype> CrystalMassGroup = "CrystalMass";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<CrystalMassComponent, ComponentStartup>(SetupCrystalMass);
        SubscribeLocalEvent<CrystalMassComponent, SpreadNeighborsEvent>(OnCrystalMassSpread);
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

        var prototype = MetaData(uid).EntityPrototype?.ID;

        if (prototype == null)
        {
            RemCompDeferred<ActiveEdgeSpreaderComponent>(uid);
            return;
        }

        foreach (var neighbor in args.NeighborFreeTiles)
        {
            var neighborUid = Spawn(prototype, _map.GridTileToLocal(neighbor.Tile.GridUid, neighbor.Grid, neighbor.Tile.GridIndices));
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
        {
            return;
        }

        _appearance.SetData(uid, CrystalMassVisuals.Variant, _robustRandom.Next(0, component.SpriteVariants), appearance);
    }
}
