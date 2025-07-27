using System.Linq;
using Content.Shared.Clothing;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Lens;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.StartWithLenses;

public sealed class StartWithLensesSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly SharedHandsSystem _sharedHandsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StartWithLensesComponent, MapInitEvent>(OnMapInit, after: [typeof(LoadoutSystem)]);
    }

    private void OnMapInit(Entity<StartWithLensesComponent> ent, ref MapInitEvent args)
    {
        if (!_inventorySystem.TryGetSlot(ent, "eyes", out var slot))
            return;

        var eyeSet = _inventorySystem.GetHandOrInventoryEntities(ent.Owner, SlotFlags.EYES);

        if (!eyeSet.Any())
            return;

        var eyes = eyeSet.First();

        if (TryComp<LensSlotComponent>(eyes, out var lensSlot))
        {
            var item = Spawn(ent.Comp.LensPrototype, Transform(ent).Coordinates);

            if (_itemSlotsSystem.TryGetSlot(eyes, lensSlot.LensSlotId, out ItemSlot? itemSlot))
                _itemSlotsSystem.TryInsert(eyes, itemSlot, item, user: null);
        }
        else
        {
            if (TryComp<HandsComponent>(ent, out var handsComponent))
            {
                var coords = Transform(ent).Coordinates;
                var inhandEntity = EntityManager.SpawnEntity(ent.Comp.LensPrototype, coords);
                _sharedHandsSystem.TryPickup(ent,
                    inhandEntity,
                    checkActionBlocker: false,
                    handsComp: handsComponent);
            }
        }

        if (eyes.Valid)
        {
            _inventorySystem.SpawnItemInSlot(ent, "eyes", ent.Comp.LensPrototype);
        }
    }
}
