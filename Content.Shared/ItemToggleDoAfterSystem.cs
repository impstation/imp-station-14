using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item.ItemToggle.Components;
using Robust.Shared.Serialization;
using Content.Shared.Verbs;

namespace Content.Shared.Item.ItemToggle;

/// <summary>
/// This handles...
/// </summary>
public sealed class ItemToggleDoAfterSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ItemToggleDoAfterComponent, UseInHandEvent>(OnUse);
        SubscribeLocalEvent<ItemToggleDoAfterComponent, GetVerbsEvent<ActivationVerb>>(OnActivateVerb);
        SubscribeLocalEvent<ItemToggleDoAfterComponent, ItemToggleDoAfterEvent>(OnDoAfter);
    }

    private void OnUse(Entity<ItemToggleDoAfterComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        var doAfterEventArgs = new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.DoAfterTime,
            new ItemToggleDoAfterEvent(),
            ent,
            target: ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
        args.Handled = true;
    }

    private void OnActivateVerb(Entity<ItemToggleDoAfterComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !TryComp<ItemToggleComponent>(ent.Owner, out var toggleComp))
            return;

        var user = args.User;
        args.Verbs.Add(new ActivationVerb()
        {
            Text = !toggleComp.Activated ? Loc.GetString(toggleComp.VerbToggleOn) : Loc.GetString(toggleComp.VerbToggleOff),
            Act = () =>
            {
                var doAfterEventArgs = new DoAfterArgs(EntityManager,
                    user,
                    ent.Comp.DoAfterTime,
                    new ItemToggleDoAfterEvent(),
                    ent,
                    target: ent)
                {
                    BreakOnMove = true,
                    BreakOnDamage = true,
                    NeedHand = true,
                };

                _doAfter.TryStartDoAfter(doAfterEventArgs);
            }
        });
    }

    private void OnDoAfter(Entity<ItemToggleDoAfterComponent> ent, ref ItemToggleDoAfterEvent args)
    {
        if (args.Cancelled || !TryComp<ItemToggleComponent>(ent.Owner, out var toggleComp))
            return;

        _toggle.Toggle((ent.Owner, toggleComp), args.User, predicted: toggleComp.Predictable);
    }
}


[Serializable, NetSerializable]
public sealed partial class ItemToggleDoAfterEvent : SimpleDoAfterEvent
{
}
