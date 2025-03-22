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

    public bool UseEldritchItem(Entity<EldritchItemComponent> item, Entity<HereticComponent> user)
    {
        if (item.Comp.Spent)
            return false;

        var doAfter = new EldritchItemDoAfterEvent();
        var dargs = new DoAfterArgs(EntityManager, user, 10f, doAfter, item, item);
        _popup.PopupEntity(Loc.GetString("heretic-item-start"), item, user);
        return _doafter.TryStartDoAfter(dargs);
    }

    private void OnUseInHand(Entity<EldritchItemComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled
        || !TryComp<HereticComponent>(args.User, out var heretic))
            return;

        args.Handled = UseEldritchItem(ent, (args.User, heretic));
    }
    private void OnDoAfter(Entity<EldritchItemComponent> ent, ref EldritchItemDoAfterEvent args)
    {
        if (args.Cancelled
        || args.Target == null
        || !TryComp<HereticComponent>(args.User, out var heretic))
            return;

        _heretic.UpdateKnowledge(args.User, heretic, 1);

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
