using Content.Shared._Shitmed.Body.Components;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;

namespace Content.Shared._Shitmed.Body.Systems;

public sealed class SharedAttachItemToSlotSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AttachItemToHandComponent, HandCountChangedEvent>(HandCountChanged);
    }

    private void HandCountChanged(Entity<AttachItemToHandComponent> ent, ref HandCountChangedEvent args)
    {
        // Return if the hand slot doesn't match or the entity lacks hands
        if (args.Hand != ent.Comp.SlotId || !TryComp<HandsComponent>(ent, out var hands))
            return;

        // Spawn Item
        var itemEntity = Spawn(ent.Comp.Item, Transform(ent).Coordinates);
        ent.Comp.ItemEntity = itemEntity;

        // Ensure attachment has the UnremovableComponent
        EntityManager.EnsureComponent<UnremoveableComponent>(itemEntity);

        // Attempt to pick up the item
        if (!_hands.TryPickup(ent, itemEntity, ent.Comp.SlotId) ||
            !_hands.TryGetHand(ent, ent.Comp.SlotId, out _, hands))
        {
            // If the hand is removed, delete the item
            Del(itemEntity);
        }
    }
}
