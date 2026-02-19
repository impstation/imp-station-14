using Content.Shared.Abilities.Mime;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Popups;

namespace Content.Shared._Impstation.Mime;

public sealed class MimeSpellbookSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MimeSpellbookComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<MimeSpellbookComponent, MimeSpellbookDoAfterEvent>(OnMimeSpellbookDoAfter);
    }

    private void OnUseInHand(Entity<MimeSpellbookComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        // make sure they have a mind
        if (!_mind.TryGetMind(args.User, out var mindId, out _))
        {
            args.Handled = true;
            return;
        }

        // if they won't learn anything, stop
        if (!ent.Comp.GivesVow && ent.Comp.Action == null)
        {
            _popup.PopupClient(Loc.GetString("mime-spell-learn-failed-nothing"), args.User, args.User);
            args.Handled = true;
            return;
        }

        // if they don't have a vow and the book won't give them one, stop
        if (!HasComp<MimePowersComponent>(mindId) && !ent.Comp.GivesVow)
        {
            _popup.PopupClient(Loc.GetString("mime-spell-learn-failed"), args.User, args.User);
            args.Handled = true;
            return;
        }

        // if they have a vow and the book would only give them a vow, stop
        if (HasComp<MimePowersComponent>(mindId) && ent.Comp is { GivesVow: true, Action: null })
        {
            _popup.PopupClient(Loc.GetString("mime-spell-learn-failed-already-vowed"), args.User, args.User);
            args.Handled = true;
            return;
        }

        // check what spells they know and if they already know the spell in the book, stop
        var mindActionContainerComp = EnsureComp<ActionsContainerComponent>(mindId);
        foreach (var action in mindActionContainerComp.Container.ContainedEntities)
        {
            var entityPrototype = MetaData(action).EntityPrototype;
            if (entityPrototype != null && entityPrototype.ID == ent.Comp.Action)
            {
                _popup.PopupClient(Loc.GetString("mime-spell-learn-failed-already-know"), args.User, args.User);
                args.Handled = true;
                return;
            }
        }

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.LearnTime, new MimeSpellbookDoAfterEvent(), ent, target: ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);

        args.Handled = true;
    }

    private void OnMimeSpellbookDoAfter<T>(Entity<MimeSpellbookComponent> ent, ref T args) where T : DoAfterEvent
    {
        if (args.Handled || args.Cancelled)
            return;

        args.Handled = true;

        if (_mind.TryGetMind(args.Args.User, out var mindId, out _))
        {
            if (ent.Comp.GivesVow)
                EnsureComp<MimePowersComponent>(mindId);

            if (ent.Comp.Action != null)
                _actionContainer.AddAction(mindId, ent.Comp.Action);

            if (ent.Comp.OneUse)
            {
                _popup.PopupClient(Loc.GetString("mime-spell-learn-one-use"), args.Args.User, args.Args.User);
                PredictedQueueDel(ent);
            }
        }
    }
}
