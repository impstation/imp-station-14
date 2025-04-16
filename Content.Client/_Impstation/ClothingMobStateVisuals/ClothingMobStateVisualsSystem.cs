using Content.Shared._Impstation.ClothingMobStateVisuals;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Mobs.Systems;
using Content.Shared.Inventory;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Content.Shared.Clothing;
using System.Linq;

namespace Content.Client._Impstation.ClothingMobStateVisuals;

public sealed partial class ClothingMobStateVisualsSystem : SharedClothingMobStateVisualsSystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothingMobStateVisualsComponent, ClothingMobStateChangedEvent>(OnClothingMobStateChanged);
        SubscribeLocalEvent<ClothingMobStateVisualsComponent, GetEquipmentVisualsEvent>(OnGetEquipmentVisuals);
    }

    private void OnClothingMobStateChanged(Entity<ClothingMobStateVisualsComponent> ent, ref ClothingMobStateChangedEvent args)
    {
        if (!_protoMan.TryIndex<SpeciesPrototype>(args.SpeciesId, out var speciesProto))
            return;
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        var enabled = _mobState.IsIncapacitated(args.Equippee);
        if (sprite != null && sprite.LayerMapTryGet(ent.Comp.SpriteLayer, out var layer))
        {
            sprite.LayerSetVisible(layer, enabled);
        }
    }

    private void OnGetEquipmentVisuals(Entity<ClothingMobStateVisualsComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        if (!TryComp(args.Equipee, out InventoryComponent? inventory))
            return;
        List<PrototypeLayerData>? layers = null;

        // attempt to get species specific data
        if (inventory != null && inventory.SpeciesId != null)
            ent.Comp.ClothingVisuals.TryGetValue($"{args.Slot}-{inventory.SpeciesId}", out layers);

        // No species specific data.  Try to default to generic data.
        if (layers == null && !ent.Comp.ClothingVisuals.TryGetValue(args.Slot, out layers))
            return;

        var i = 0;
        foreach (var layer in layers)
        {
            var key = layer.MapKeys?.FirstOrDefault();
            if (key == null)
            {
                key = i == 0 ? $"{args.Slot}-{ent.Comp.SpriteLayer}" : $"{args.Slot}-{ent.Comp.SpriteLayer}-{i}";
                i++;
            }

            args.Layers.Add((key, layer));
        }
    }
}
