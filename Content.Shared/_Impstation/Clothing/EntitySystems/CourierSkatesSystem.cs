using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Movement.Components;

namespace Content.Shared.Clothing;

/// <summary>
/// Unique skate system for the courier skates that does not damage player upon collision.
/// </summary>
public sealed class CourierSkatesSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _move = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CourierSkatesComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<CourierSkatesComponent, ClothingGotUnequippedEvent>(OnGotUnequipped);
    }

    public void OnGotUnequipped(EntityUid uid, CourierSkatesComponent component, ClothingGotUnequippedEvent args)
    {
        if (!TryComp(args.Wearer, out MovementSpeedModifierComponent? speedModifier))
            return;

        _move.ChangeFriction(args.Wearer, MovementSpeedModifierComponent.DefaultFriction, MovementSpeedModifierComponent.DefaultFrictionNoInput, MovementSpeedModifierComponent.DefaultAcceleration, speedModifier);
    }

    private void OnGotEquipped(EntityUid uid, CourierSkatesComponent component, ClothingGotEquippedEvent args)
    {
        _move.ChangeFriction(args.Wearer, component.Friction, component.FrictionNoInput, component.Acceleration);
    }
}
