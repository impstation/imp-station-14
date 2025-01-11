using System.Linq;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Body.Components;

public sealed class SharedAttachItemToSlotSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AttachItemToHandComponent, HandCountChangedEvent>(HandCountChanged);
    }

    private void HandCountChanged(EntityUid uid, AttachItemToHandComponent component, HandCountChangedEvent args)
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
            //if the hand this item should be in no longer exists
            Del(component.ItemEntity);
        }
    }
}
