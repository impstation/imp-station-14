using Content.Shared._Impstation.Homunculi.Incubator.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Humanoid;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Wires;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._Impstation.Homunculi.Incubator;

public abstract class SharedIncubatorSystem : EntitySystem
{
    [Dependency] private readonly ItemToggleSystem _toggle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IncubatorComponent, ItemSlotEjectAttemptEvent>(OnItemSlotEjectAttempt);
    }

    [Serializable, NetSerializable]
    public sealed class HomunculiColorsChangedEvent : EntityEventArgs
    {
        public readonly (Color skinColor, Color eyeColor) Colors;
        public readonly NetEntity Homunculus;

        public HomunculiColorsChangedEvent(Color skinColor, Color eyeColor, NetEntity homunculus)
        {
            Homunculus = homunculus;
            Colors.skinColor = skinColor;
            Colors.eyeColor = eyeColor;
        }
    }
    private void OnItemSlotEjectAttempt(Entity<IncubatorComponent> ent, ref ItemSlotEjectAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (_toggle.IsActivated(ent.Owner))
            args.Cancelled = true;
    }

}
