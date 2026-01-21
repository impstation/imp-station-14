using Content.Shared.Clothing.Components;
using Content.Shared.Examine;
using Content.Shared.Glue;
using Content.Shared.Inventory;
using Content.Shared.Random.Helpers;
using Content.Shared.Standing;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Impstation.SecurelyAttached;

public sealed class SecurelyAttachedSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly ThrowingSystem _throwingSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<InventoryComponent, DropEquipmentAttemptEvent>(DropEquipment);
        SubscribeLocalEvent<ClothingComponent, ExaminedEvent>(OnExamined);
    }

    // Ran when entity is tripped, or is thrown
    private void DropEquipment(Entity<InventoryComponent> ent, ref DropEquipmentAttemptEvent args)
    {
        foreach (var slot in ent.Comp.Slots)
        {
            if (!_inventory.TryGetSlotEntity(ent, slot.Name, out var itemUid))
                continue;

            if (!TryComp<ClothingComponent>(itemUid, out var clothing))
                return;

            var seed = SharedRandomExtensions.HashCodeCombine(new List<int>
                { (int)_timing.CurTick.Value, GetNetEntity(ent).Id });
            var rand = new System.Random(seed);

            var dropChance = clothing.InsecureDropChance;

            var playerPosition = _physics.GetPhysicsTransform(ent).Position;
            var linearVelocity = _physics.GetLinearVelocity(ent, playerPosition);

            dropChance *= 1 + linearVelocity.Length() / clothing.FallSpeedModifier;
            var direction = Transform(ent).LocalRotation;
            direction += 0;
            if (!(rand.NextFloat() <= dropChance))
                continue;

            if (!clothing.Insecure)
                continue;

            if (!_inventory.TryUnequip(ent, slot.Name, silent: true, checkDoafter: false, predicted: true))
                continue;

            // this is all taken from hands system. thank you hands system.

            var throwAttempt = new FellDownThrowAttemptEvent(ent);
            RaiseLocalEvent(itemUid.Value, ref throwAttempt);

            if (throwAttempt.Cancelled)
                return;

            _throwingSystem.TryThrow(itemUid.Value, direction.ToVec().Normalized() * clothing.DirectionMultiplier, linearVelocity.Length(), ent, compensateFriction: !HasComp<LandAtCursorComponent>(itemUid.Value));
        }
    }

    private void OnExamined(Entity<ClothingComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange|| ent.Comp.InSlot == null)
            return;

        var secure = !ent.Comp.Insecure || HasComp<GluedComponent>(ent) || HasComp<AttachedClothingComponent>(ent);

        args.PushMarkup(secure ? Loc.GetString("securely-attached") : Loc.GetString("insecurely-attached"));
    }

    public sealed class DropEquipmentAttemptEvent : EntityEventArgs;
}
