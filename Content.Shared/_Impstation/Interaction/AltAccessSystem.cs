using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Robust.Shared.Player;

namespace Content.Shared._Impstation.Interaction;

public sealed class AltAccessSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AltAccessComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AltAccessComponent, GetItemActionsEvent>(OnGetActions);

        SubscribeLocalEvent<AltAccessComponent, AltAccessPdaScreenEvent>(OnTogglePdaScreen);

    }

    private void OnMapInit(Entity<AltAccessComponent> ent, ref MapInitEvent args)
    {
        var (uid, comp) = ent;
        if (comp.AltAccessAction is null)
            return;

        _actionContainer.EnsureAction(uid, ref comp.AltAccessEntity, comp.AltAccessAction);
        Dirty(uid, comp);
    }

    private void OnGetActions(Entity<AltAccessComponent> ent, ref GetItemActionsEvent args)
    {
        var (uid, comp) = ent;

        if (_actionBlockerSystem.CanInteract(args.User, uid) && _actionBlockerSystem.CanComplexInteract(args.User))
            return;

        if (_inventorySystem.InSlotWithFlags(uid, comp.SlotFlags))
        {
            args.AddAction(comp.AltAccessEntity);
            Dirty(uid, comp);
        }
    }

    // TODO: Look into generalizing more later, but for now, each action requires its own event and handler function
    private void OnTogglePdaScreen(Entity<AltAccessComponent> ent, ref AltAccessPdaScreenEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        EntityUid? wearer = null;
        if (_inventorySystem.InSlotWithFlags(ent.Owner, SlotFlags.IDCARD))
        {
            wearer = Transform(ent).ParentUid;
        }

        if (wearer != null && TryComp<ActorComponent>(wearer, out var actor))
        {
            _userInterface.TryToggleUi(ent.Owner, PdaUiKey.Key, actor.PlayerSession);
        }
    }
}

public sealed partial class AltAccessPdaScreenEvent : InstantActionEvent;
