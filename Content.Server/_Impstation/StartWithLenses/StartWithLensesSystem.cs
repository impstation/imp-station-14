using System.Linq;
using Content.Shared.Clothing;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Lens;
using Robust.Shared.Containers;

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

    private void OnMapInit(EntityUid uid, StartWithLensesComponent component, MapInitEvent args)
    {
        if (_inventorySystem.TryGetSlot(uid, "eyes", out var slot))
        {
            var eyes = _inventorySystem.GetHandOrInventoryEntities(uid, SlotFlags.EYES).First();

            var lensSlot = CompOrNull<LensSlotComponent>(eyes);

            if (lensSlot is not null)
            {
                var item = Spawn("PrescriptionLensStrong", Transform(uid).Coordinates);

                if (_itemSlotsSystem.TryGetSlot(eyes, lensSlot.LensSlotId, out ItemSlot? itemSlot))
                    _itemSlotsSystem.TryInsert(eyes, itemSlot, item, user: null);
            }
            else
            {
                if (TryComp(uid, out HandsComponent? handsComponent))
                {
                    var coords = Transform(uid).Coordinates;
                    var inhandEntity = EntityManager.SpawnEntity("PrescriptionLensStrong", coords);
                    _sharedHandsSystem.TryPickup(uid,
                        inhandEntity,
                        checkActionBlocker: false,
                        handsComp: handsComponent);
                }
            }

            if (eyes.Valid)
            {
                _inventorySystem.SpawnItemInSlot(uid, "eyes", "PrescriptionLensStrong");
            }
        }
    }
}
