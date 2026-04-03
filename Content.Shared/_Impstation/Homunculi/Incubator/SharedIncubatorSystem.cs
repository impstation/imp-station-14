using Content.Shared.Containers.ItemSlots;
using Content.Shared.Item.ItemToggle;
using Content.Shared._Impstation.Homunculi.Incubator.Components;

namespace Content.Shared._Impstation.Homunculi.Incubator;

public abstract class SharedIncubatorSystem : EntitySystem
{
    [Dependency] private readonly ItemToggleSystem _toggle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IncubatorComponent, ItemSlotEjectAttemptEvent>(OnItemSlotEjectAttempt);
    }

    private void OnItemSlotEjectAttempt(Entity<IncubatorComponent> ent, ref ItemSlotEjectAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (_toggle.IsActivated(ent.Owner))
            args.Cancelled = true;
    }
}
