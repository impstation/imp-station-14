using Content.Shared.Inventory;

namespace Content.Shared._EE.Movement.Events;

public record struct MakeFootstepSoundEvent : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.All;
}
