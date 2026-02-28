using Content.Shared.Actions.Events;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;

namespace Content.Shared._Impstation.Actions;

public sealed class PopupOnActionSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PopupOnActionComponent, ActionPerformedEvent>(OnActionPerformed);
    }

    private void OnActionPerformed(Entity<PopupOnActionComponent> ent, ref ActionPerformedEvent args)
    {
        var user = Identity.Name(args.Performer, EntityManager);

        // Popups only play for one entity
        if (ent.Comp.Quiet)
        {
            if (ent.Comp.Predicted)
            {
                _popup.PopupClient(Loc.GetString(ent.Comp.Text, ("entity", ent), ("user", user)),
                    ent.Comp.UserIsRecipient ? args.Performer : ent.Owner,
                    ent.Comp.PopupType);
            }

            else
            {
                _popup.PopupEntity(Loc.GetString(ent.Comp.OtherText ?? ent.Comp.Text, ("entity", ent), ("user", user)),
                    args.Performer,
                    ent.Comp.PopupType);
            }

            return;
        }

        // Popups play for all entities
        if (ent.Comp.Predicted)
        {
            _popup.PopupPredicted(
                Loc.GetString(ent.Comp.Text, ("entity", ent), ("user", user)),
                Loc.GetString(ent.Comp.OtherText ?? ent.Comp.Text, ("entity", ent), ("user", user)),
                ent.Comp.UserIsRecipient ? args.Performer : ent.Owner,
                ent.Comp.UserIsRecipient ? args.Performer : ent.Owner,
                ent.Comp.PopupType);
        }

        else
        {
            _popup.PopupEntity(Loc.GetString(ent.Comp.OtherText ?? ent.Comp.Text, ("entity", ent), ("user", user)),
                args.Performer,
                ent.Comp.PopupType);
        }
    }
}
