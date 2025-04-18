using Content.Shared._Impstation.ClothingMobStateVisuals;
using Content.Shared.Item;

namespace Content.Client._Impstation.ClothingMobStateVisuals;

public sealed partial class ClothingMobStateVisualsSystem : SharedClothingMobStateVisualsSystem
{
    [Dependency] private readonly SharedItemSystem _itemSys = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothingMobStateVisualsComponent, ClothingMobStateChangedEvent>(OnClothingMobStateChanged);
    }

    private void OnClothingMobStateChanged(Entity<ClothingMobStateVisualsComponent> ent, ref ClothingMobStateChangedEvent args)
    {
        _itemSys.VisualsChanged(ent);
    }
}
