using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;

namespace Content.Shared._Shitmed.Body.Systems;

public sealed class SharedAttachItemToSlotSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<_Shitmed.Body.Components.AttachItemToHandComponent, HandCountChangedEvent>(HandCountChanged);
    }

    private void HandCountChanged(EntityUid uid, _Shitmed.Body.Components.AttachItemToHandComponent component, HandCountChangedEvent args)
    {
        // Check if the hand slot matches and the entity has a HandsComponent
        if (args.Hand != component.SlotId || !TryComp<HandsComponent>(uid, out var hands))
            return;

        // Attempt to get the TransformComponent
        if (!EntityManager.TryGetComponent(uid, out TransformComponent? transformComponent))
            return;

        // Spawn the attachment entity and attempt to pick it up
        component.ItemEntity = EntityManager.SpawnEntity(component.Item, transformComponent.Coordinates);
        _handsSystem.TryPickup(uid, component.ItemEntity, component.SlotId);

        if (!_handsSystem.TryGetHand(uid, component.SlotId, out _, hands))
        {
            //if the hand is removed, this item should no longer exist
            Del(component.ItemEntity);
        }
    }
}
