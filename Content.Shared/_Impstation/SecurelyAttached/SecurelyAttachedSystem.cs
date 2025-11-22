using Content.Shared._Impstation.SecurelyAttached.Components;
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
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ThrowingSystem _throwingSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

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

    /// <summary>
    /// Drop chance is increased by linear velocity divided by this number.
    /// </summary>
    private const float SpeedModifier = 15f;

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

            var seed = SharedRandomExtensions.HashCodeCombine(new() { (int)_timing.CurTick.Value, GetNetEntity(itemUid.Value).Id });
            var rand = new System.Random(seed);

            var dropChance = slot.InsecureDropChance;

            var playerPosition = _physics.GetPhysicsTransform(ent).Position;
            var linearVelocity = _physics.GetLinearVelocity(ent, playerPosition).Length();

            dropChance *= 1 + linearVelocity / SpeedModifier;

            if (!(rand.NextFloat() <= dropChance))
                continue;

            if (!slot.Insecure || HasComp<SecurelyAttachedComponent>(itemUid))
                continue;

            if (!_inventory.TryUnequip(ent, slot.Name, silent: true, checkDoafter: false, predicted: true))
                continue;

            // this is all taken from hands system. thank you hands system.

            var offsetRandomCoordinates = _transform.GetMoverCoordinates(ent).Offset(rand.NextPolarVector2(0f, 0.5f));
            var direction = _transform.ToMapCoordinates(offsetRandomCoordinates).Position - _transform.GetWorldPosition(ent);

            var length = direction.Length();
            var distance = Math.Clamp(length, MinimumDistance, MaximumDistance);
            direction *= distance / length;

            var throwAttempt = new FellDownThrowAttemptEvent(ent);
            RaiseLocalEvent(itemUid.Value, ref throwAttempt);

            if (throwAttempt.Cancelled)
                return;

            _throwingSystem.TryThrow(itemUid.Value, direction, ItemThrowSpeed, ent, compensateFriction: !HasComp<LandAtCursorComponent>(itemUid.Value));
        }
    }

    private void OnExamined(Entity<ClothingComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange|| !_inventory.TryGetContainingSlot(ent.Owner, out var slot))
            return;

        var secure = !slot.Insecure || HasComp<GluedComponent>(ent) || HasComp<AttachedClothingComponent>(ent);

        if (secure || HasComp<SecurelyAttachedComponent>(ent))
            args.PushMarkup(Loc.GetString("securely-attached"));
        else
            args.PushMarkup(Loc.GetString("insecurely-attached"));
    }
}
