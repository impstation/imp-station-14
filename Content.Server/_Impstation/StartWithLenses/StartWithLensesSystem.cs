using System.Linq;
using Content.Server._Impstation.TraitRandomizer;
using Content.Server.Bed.Cryostorage;
using Content.Server.Traits;
using Content.Shared.Clothing;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.GameTicking;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Lens;

namespace Content.Server._Impstation.StartWithLenses;

public sealed class StartWithLensesSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly SharedHandsSystem _sharedHandsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete, after: [typeof(TraitSystem), typeof(LoadoutSystem), typeof(TraitRandomizerSystem), typeof(CryostorageSystem)]);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (!TryComp<StartWithLensesComponent>(args.Mob, out var comp))
        {
            return;
        }

        // spawn it on the floor first Just In Case
        var spawnedLens = Spawn(comp.LensPrototype, Transform(args.Mob).Coordinates);

        // check if the entity has an eyes slot
        if (_inventorySystem.TryGetSlot(args.Mob, "eyes", out var slotDefinition))
        {
            // get the item in the eyes slot
            var eyeSet = (_inventorySystem.GetHandOrInventoryEntities(args.Mob, SlotFlags.EYES));

            // check if there's anything in the eye slot
            if (!eyeSet.Any())
            {
                // if not, spawn them in the eyes slot
                if (_inventorySystem.SpawnItemInSlot(args.Mob, "eyes", comp.LensPrototype))
                {
                    // delete the one on the floor
                    Del(spawnedLens);
                    return;
                }
            }

            // there's something there, get it
            if (_inventorySystem.TryGetSlotEntity(args.Mob, "eyes", out var slotEntity))
            {
                // check if it has a lens slot component
                if (TryComp<LensSlotComponent>(slotEntity, out var lensSlotComponent))
                {
                    if (_itemSlotsSystem.TryGetSlot(slotEntity.Value, lensSlotComponent.LensSlotId, out ItemSlot? lensSlot))
                    {
                        // if there's something already in the lens slot, delete it
                        if (lensSlot.Item != null)
                        {
                            Del(lensSlot.Item);
                        }
                        if (_itemSlotsSystem.TryInsert(slotEntity.Value, lensSlot, spawnedLens, user: null))
                        {
                            return;
                        }
                    }
                }
            }
        }

        // eye slot didn't work, it's hand time
        // check if the entity has hands
        if (TryComp<HandsComponent>(args.Mob, out var handsComponent))
        {
            // try to put it in their hands
            _sharedHandsSystem.TryPickup(args.Mob, spawnedLens, checkActionBlocker: false, handsComp: handsComponent);
        }
    }
}
