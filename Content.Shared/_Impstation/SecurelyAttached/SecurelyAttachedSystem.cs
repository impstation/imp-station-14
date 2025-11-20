using System.Numerics;
using Content.Shared._Impstation.SecurelyAttached.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Clothing.Components;
using Content.Shared.Examine;
using Content.Shared.Glue;
using Content.Shared.Inventory;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Standing;
using Content.Shared.Throwing;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._Impstation.SecurelyAttached;

public sealed class SecurelyAttachedSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ThrowingSystem _throwingSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<PhysicsComponent> _physicsQuery;

    /// <summary>
    /// Speed items are thrown off of the holder
    /// </summary>
    private const float ItemThrowSpeed = 5;

    /// <summary>
    /// Minimum distance an item can be thrown
    /// </summary>
    private const float MinimumDistance = 0.1f;

    /// <summary>
    /// Maximum distance an item can be thrown
    /// </summary>
    private const float MaximumDistance = 2f;

    public override void Initialize()
    {
        SubscribeLocalEvent<ClothingComponent, ExaminedEvent>(OnExamined);
    }

    // Ran when entity is tripped, or is thrown
    public void DropEquipment(Entity<InventoryComponent> ent)
    {
        foreach (var slot in ent.Comp.Slots)
        {
            if (!_inventory.TryGetSlotEntity(ent, slot.Name, out var itemUid))
                continue;

            var dropChance = slot.InsecureDropChance;

            if (!slot.Insecure || HasComp<SecurelyAttachedComponent>(itemUid))
                continue;

            if (!(_random.NextFloat() <= dropChance))
                continue;

            // this is all taken from hands system. thank you hands system.

            var offsetRandomCoordinates = _transform.GetMoverCoordinates(ent).Offset(_random.NextVector2(1f, 1.5f));

            var direction = _transform.ToMapCoordinates(offsetRandomCoordinates).Position - _transform.GetWorldPosition(ent);

            var length = direction.Length();
            var distance = Math.Clamp(length, MinimumDistance, MaximumDistance);
            direction *= distance / length;

            var itemEv = new BeforeGettingThrownEvent(itemUid.Value, direction, ItemThrowSpeed, ent);
            RaiseLocalEvent(itemUid.Value, ref itemEv);

            if (itemEv.Cancelled)
                return;

            var ev = new BeforeThrowEvent(itemUid.Value, direction, ItemThrowSpeed, ent);
            RaiseLocalEvent(ent, ref ev);

            if (ev.Cancelled)
                return;

            _throwingSystem.TryThrow(ev.ItemUid, ev.Direction, ev.ThrowSpeed, ev.PlayerUid, compensateFriction: !HasComp<LandAtCursorComponent>(ev.ItemUid));
        }
    }

    public bool TryDrop(EntityUid wearer, EntityUid itemToDrop, string slotName)
    {
        return _inventory.CanUnequip(wearer, slotName, out _);
    }

    private void OnExamined(Entity<ClothingComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || HasComp<GluedComponent>(ent) || !_inventory.TryGetContainingSlot(ent.Owner, out var slot))
            return;

        if (slot.Insecure && !HasComp<SecurelyAttachedComponent>(ent))
            args.PushMarkup(Loc.GetString("insecure-attached"));
        else
            args.PushMarkup(Loc.GetString("securely-attached"));
    }
}
