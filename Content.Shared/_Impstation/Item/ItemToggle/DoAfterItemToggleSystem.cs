using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared._Impstation.Item.ItemToggle.Components;
using Content.Shared.Verbs;

namespace Content.Shared._Impstation.Item.ItemToggle;

/// <summary>
///     Handles do-afters for generic item toggles.
///     Actual toggling is handled by <see cref="ItemToggleSystem"/>.
/// </summary>
public sealed class Imp_DoAfterItemToggleSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<Imp_DoAfterItemToggleComponent, UseInHandEvent>(OnUse);
        SubscribeLocalEvent<Imp_DoAfterItemToggleComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<Imp_DoAfterItemToggleComponent, GetVerbsEvent<ActivationVerb>>(OnActivateVerb);

        SubscribeLocalEvent<ItemToggleComponent, ItemToggleDoAfterEvent>(OnDoAfter);
    }

    private void OnUse(Entity<Imp_DoAfterItemToggleComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || !ent.Comp.OnUse)
            return;

        args.Handled = true;
        StartDoAfter(ent, args.User);
    }

    private void OnActivate(Entity<Imp_DoAfterItemToggleComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !ent.Comp.OnActivate)
            return;

        args.Handled = true;
        StartDoAfter(ent, args.User);
    }

    private void OnActivateVerb(Entity<Imp_DoAfterItemToggleComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !ent.Comp.OnActivate)
            return;

        if (!TryComp<ItemToggleComponent>(ent.Owner, out var toggleComp))
            return;

        // Check if we can activate/deactivate before adding the verb.
        if (toggleComp.Activated)
        {
            var ev = new ItemToggleActivateAttemptEvent(args.User);
            RaiseLocalEvent(ent.Owner, ref ev);

            if (ev.Cancelled)
                return;
        }
        else
        {
            var ev = new ItemToggleDeactivateAttemptEvent(args.User);
            RaiseLocalEvent(ent.Owner, ref ev);

            if (ev.Cancelled)
                return;
        }

        var user = args.User; // can't pass by ref into verbs
        args.Verbs.Add(new ActivationVerb()
        {
            Text = !toggleComp.Activated ? Loc.GetString(toggleComp.VerbToggleOn) : Loc.GetString(toggleComp.VerbToggleOff),
            Act = () =>
            {
                StartDoAfter(ent, user);
            },
        });
    }

    private void OnDoAfter(Entity<ItemToggleComponent> ent, ref ItemToggleDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        // Where the magic happens
        _toggle.Toggle(ent.AsNullable(), args.User, ent.Comp.Predictable);
    }

    /// <summary>
    /// Shared method for the do-after call.
    /// </summary>
    /// <param name="ent">Entity getting toggled.</param>
    /// <param name="user">Entity performing the do-after.</param>
    private void StartDoAfter(Entity<Imp_DoAfterItemToggleComponent> ent, EntityUid user)
    {
        var doAfterEventArgs = new DoAfterArgs(EntityManager,
                                                user,
                                                ent.Comp.DoAfterTime,
                                                new ItemToggleDoAfterEvent(),
                                                ent,
                                                target: ent)
        {
            NeedHand = ent.Comp.NeedHand,
            BreakOnMove = ent.Comp.BreakOnMove,
            BreakOnDamage = ent.Comp.BreakOnDamage,
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
    }
}
