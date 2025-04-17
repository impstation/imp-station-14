using Content.Shared._Impstation.ClothingMobStateVisuals;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Mobs.Systems;
using Content.Shared.Inventory;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Content.Shared.Clothing;
using Content.Shared.Item;
using System.Linq;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;

namespace Content.Client._Impstation.ClothingMobStateVisuals;

public sealed partial class ClothingMobStateVisualsSystem : SharedClothingMobStateVisualsSystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedItemSystem _itemSys = default!;
    [Dependency] private readonly ClothingSystem _clothing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothingMobStateVisualsComponent, ClothingMobStateChangedEvent>(OnClothingMobStateChanged);
        SubscribeLocalEvent<ClothingMobStateVisualsComponent, GetEquipmentVisualsEvent>(OnGetEquipmentVisuals);
    }



    private void OnClothingMobStateChanged(Entity<ClothingMobStateVisualsComponent> ent, ref ClothingMobStateChangedEvent args)
    {
        if (!_protoMan.TryIndex(args.SpeciesId, out _))
            return;
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        var enabled = _mobState.IsIncapacitated(args.Equippee);
        if (sprite != null && sprite.LayerMapTryGet(ent.Comp.SpriteLayer, out var layer))
        {
            sprite.LayerSetVisible(layer, enabled);
        }
        _itemSys.VisualsChanged(ent);
    }

    private void OnGetEquipmentVisuals(Entity<ClothingMobStateVisualsComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        if (!TryComp(args.Equipee, out InventoryComponent? inventory) || !TryComp(ent, out ClothingComponent? clothingComp))
            return;

        if (ent.Comp.ClothingPrefix == null)
            ent.Comp.ClothingPrefix = clothingComp.EquippedPrefix;

        var prefix = _mobState.IsIncapacitated(args.Equipee) ? ent.Comp.IncapacitatedPrefix : ent.Comp.ClothingPrefix;
        _clothing.SetEquippedPrefix(ent, prefix, clothingComp);
    }
}
