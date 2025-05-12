using Content.Server.Popups;
using Content.Shared.Storage.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Movement.Events;
using Content.Shared.Resist;
using Content.Shared.Storage;
using Robust.Shared.Containers;

namespace Content.Server.Resist;

public sealed class EscapeInventorySystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CanEscapeInventoryComponent, MoveInputEvent>(OnRelayMovement);
        SubscribeLocalEvent<CanEscapeInventoryComponent, EscapeInventoryEvent>(OnEscape);
        SubscribeLocalEvent<CanEscapeInventoryComponent, DroppedEvent>(OnDropped);
    }

    private void OnRelayMovement(Entity<CanEscapeInventoryComponent> ent, ref MoveInputEvent args)
    {
        if (!args.HasDirectionalMovement)
            return;

        if (!_containerSystem.TryGetContainingContainer((ent, null, null), out var container) || !_actionBlockerSystem.CanInteract(ent, container.Owner))
            return;

        // Make sure there's nothing stopped the removal (like being glued)
        if (!_containerSystem.CanRemove(ent, container))
        {
            _popupSystem.PopupEntity(Loc.GetString("escape-inventory-component-failed-resisting"), ent, ent);
            return;
        }

        // Contested
        if (_handsSystem.IsHolding(container.Owner, ent, out _))
        {
            AttemptEscape(ent, container.Owner);
            return;
        }

        // Uncontested
        if (HasComp<StorageComponent>(container.Owner) || HasComp<InventoryComponent>(container.Owner) || HasComp<SecretStashComponent>(container.Owner))
            AttemptEscape(ent, container.Owner);
    }

    public void AttemptEscape(Entity<CanEscapeInventoryComponent> ent, EntityUid container, float multiplier = 1f) // imp edit
    {
        if (ent.Comp.IsEscaping)
            return;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, ent, ent.Comp.BaseResistTime * multiplier, new EscapeInventoryEvent(), ent, target: container)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = false
        };

        if (!_doAfterSystem.TryStartDoAfter(doAfterEventArgs, out ent.Comp.DoAfter))
            return;

        _popupSystem.PopupEntity(Loc.GetString("escape-inventory-component-start-resisting"), ent, ent);
        _popupSystem.PopupEntity(Loc.GetString("escape-inventory-component-start-resisting-target"), container, container);
    }

    private void OnEscape(Entity<CanEscapeInventoryComponent> ent, ref EscapeInventoryEvent args)
    {
        ent.Comp.DoAfter = null;

        if (args.Handled || args.Cancelled)
            return;

        _containerSystem.AttachParentToContainerOrGrid((ent, Transform(ent)));
        args.Handled = true;
    }

    private void OnDropped(Entity<CanEscapeInventoryComponent> ent, ref DroppedEvent args)
    {
        if (ent.Comp.DoAfter != null)
            _doAfterSystem.Cancel(ent.Comp.DoAfter);
    }
}
