using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Item;
using Content.Shared.Humanoid;

namespace Content.Shared._Impstation.ClothingMobStateVisuals;

public abstract class SharedClothingMobStateVisualsSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedItemSystem _itemSys = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothingMobStateVisualsComponent, InventoryRelayedEvent<MobStateChangedEvent>>(OnMobStateChanged);
    }

    private void OnMobStateChanged(Entity<ClothingMobStateVisualsComponent> ent, ref InventoryRelayedEvent<MobStateChangedEvent> args)
    {
        if (!TryComp<HumanoidAppearanceComponent>(args.Args.Target, out var humanoidAppearance))
            return;

        _itemSys.VisualsChanged(ent);

        var ev = new ClothingMobStateChangedEvent(ent, args.Args.Target, humanoidAppearance.Species, args.Args.NewMobState);
        RaiseLocalEvent(ev);
    }
}
