using Content.Server.Heretic.Components;
using Content.Server.Popups;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Heretic;
using Content.Shared.Interaction.Events;

namespace Content.Server.Heretic.EntitySystems;

public sealed partial class EldritchItemSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doafter = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly HereticSystem _heretic = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EldritchItemComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<EldritchItemComponent, EldritchItemDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<EldritchItemComponent, ExaminedEvent>(OnExamined);
    }

    private void OnUseInHand(Entity<EldritchItemComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || !TryComp<HereticComponent>(args.User, out var heretic))
            return;

        var user = args.User;
        if (ent.Comp.Spent)
            return;

        var dargs = new DoAfterArgs(EntityManager, args.User, 10f, new EldritchItemDoAfterEvent(), ent, used: ent);
        _popup.PopupEntity(Loc.GetString("heretic-item-start"), ent, user);
        _doafter.TryStartDoAfter(dargs);
        args.Handled = true;
    }
    private void OnDoAfter(Entity<EldritchItemComponent> ent, ref EldritchItemDoAfterEvent args)
    {
        if (args.Cancelled || !TryComp<HereticComponent>(args.User, out var heretic))
        {
            return;
        }
        _heretic.UpdateKnowledge(args.User, heretic, 1f);

        ent.Comp.Spent = true;
    }

    private void OnExamined(Entity<EldritchItemComponent> ent, ref ExaminedEvent args)
    {
        if(TryComp<HereticComponent>(args.Examiner, out _))
        {
            if (ent.Comp.Spent)
            {
                args.PushMarkup(markup: $"[color=purple]{Loc.GetString("heretic-item-examine-spent")}[/color]");
            }
            else
            {
                args.PushMarkup(markup: $"[color=purple]{Loc.GetString("heretic-item-examine-unspent")}[/color]");
            }
        }
        else
        {
            args.PushMarkup(markup: $"[color=purple]{Loc.GetString("heretic-item-examine-nonheretic")}[/color]");
        }
    }
}
