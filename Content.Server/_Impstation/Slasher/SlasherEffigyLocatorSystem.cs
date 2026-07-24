using Content.Server._Impstation.Slasher.Components;
using Content.Server._Impstation.GameTicking.Rules;
using Content.Server.Hands.Systems;
using Content.Server.Pinpointer;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Pinpointer;
using Content.Shared.Whitelist;
using Content.Shared._Impstation.Slasher;
using Content.Shared._Impstation.Slasher.Components;

namespace Content.Server._Impstation.Slasher;

/// <summary>
/// Effigy locator system.
/// </summary>
public sealed class SlasherEffigyLocatorSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly PinpointerSystem _pinpointer = default!;
    [Dependency] private readonly SlasherRuleSystem _rule = default!;

    /// <summary>
    /// Subscribe to the locator events this system uses.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlasherRoleComponent, SlasherEffigyLocatorChangedEvent>(OnLocatorAvailabilityChanged);
        SubscribeLocalEvent<SlasherEffigyLocatorComponent, ComponentShutdown>(OnLocatorShutdown);
        SubscribeLocalEvent<SlasherLocateEffigyActionComponent, SlasherLocateEffigyEvent>(OnLocateEffigy);
    }

    /// <summary>
    /// Update the locator action when the active effigy changes.
    /// </summary>
    /// <param name="ent">The Slasher role entity.</param>
    /// <param name="args">The current effigy state.</param>
    private void OnLocatorAvailabilityChanged(Entity<SlasherRoleComponent> ent, ref SlasherEffigyLocatorChangedEvent args)
    {
        if (!TryComp<SlasherEffigyLocatorComponent>(ent.Owner, out var locator))
            return;

        if (args.Effigy is not { } effigy || !Exists(effigy))
        {
            RevokeLocateEffigyAction(ent, locator);
            return;
        }

        EnsureLocateEffigyAction(ent, locator);
    }

    /// <summary>
    /// Ensure the locate-effigy action exists.
    /// </summary>
    /// <param name="ent">The Slasher role entity that should own the action.</param>
    /// <param name="locator">The Slasher's locator component.</param>
    private void EnsureLocateEffigyAction(Entity<SlasherRoleComponent> ent, SlasherEffigyLocatorComponent locator)
    {
        if (locator.ActionEntity is { } existingAction && Exists(existingAction))
            return;

        EntityUid? actionEntity = null;
        if (!_actions.AddAction(ent.Owner, ref actionEntity, locator.ActionPrototype)
            || actionEntity is not { } addedAction)
        {
            return;
        }

        locator.ActionEntity = addedAction;
        ent.Comp.ActionEntities.Add(addedAction);
    }

    /// <summary>
    /// Remove the locate-effigy action and clear locator state.
    /// </summary>
    /// <param name="ent">The Slasher role entity losing the action.</param>
    /// <param name="locator">The Slasher's locator component.</param>
    private void RevokeLocateEffigyAction(Entity<SlasherRoleComponent> ent, SlasherEffigyLocatorComponent locator)
    {
        DeactivateEffigyLocator(ent.Owner, locator);

        if (locator.ActionEntity is { } actionEntity && Exists(actionEntity))
            _actions.RemoveAction(ent.Owner, actionEntity);

        if (locator.ActionEntity is { } trackedAction)
            ent.Comp.ActionEntities.Remove(trackedAction);

        locator.ActionEntity = null;
    }

    /// <summary>
    /// Toggle the temporary locator hand.
    /// </summary>
    /// <param name="ent">The action entity that was invoked.</param>
    /// <param name="args">The action event data.</param>
    private void OnLocateEffigy(Entity<SlasherLocateEffigyActionComponent> ent, ref SlasherLocateEffigyEvent args)
    {
        if (args.Handled
            || !TryComp<SlasherEffigyLocatorComponent>(args.Performer, out var locator)
            || !TryComp<ActionComponent>(ent.Owner, out var action))
        {
            return;
        }

        if (!_rule.TryGetActiveRule(out var rule)
            || rule.Comp.ActiveEffigy is not { } effigy
            || !Exists(effigy))
        {
            DeactivateEffigyLocator(args.Performer, locator);
            _actions.SetToggled((ent.Owner, action), false);
            args.Handled = true;
            return;
        }

        if (locator.PinpointerEntity is { } pinpointer && Exists(pinpointer))
        {
            DeactivateEffigyLocator(args.Performer, locator);
            _actions.SetToggled((ent.Owner, action), false);
            args.Handled = true;
            return;
        }

        if (!ActivateEffigyLocator(args.Performer, effigy, locator))
            return;

        _actions.SetToggled((ent.Owner, action), true);
        args.Handled = true;
    }

    /// <summary>
    /// Add the temporary hand and place a locator pinpointer in it.
    /// </summary>
    /// <param name="performer">The Slasher using the locator.</param>
    /// <param name="effigy">The effigy that the spawned pinpointer should track.</param>
    /// <param name="locator">The Slasher's locator component.</param>
    /// <returns><see langword="true"/> if setup succeeds.</returns>
    private bool ActivateEffigyLocator(EntityUid performer, EntityUid effigy, SlasherEffigyLocatorComponent locator)
    {
        if (!TryComp<HandsComponent>(performer, out var hands))
            return false;

        if (!_hands.TryGetHand((performer, hands), locator.HandId, out _))
        {
            _hands.AddHand((performer, hands), locator.HandId, new Hand(
                HandLocation.Middle,
                emptyRepresentative: locator.PinpointerPrototype,
                whitelist: new EntityWhitelist
                {
                    Tags = ["SlasherEffigyLocator"]
                }));
        }

        var pinpointerEntity = Spawn(locator.PinpointerPrototype, Transform(performer).Coordinates);

        if (!_hands.TryPickup(performer, pinpointerEntity, locator.HandId, checkActionBlocker: false, animate: false, handsComp: hands))
        {
            Del(pinpointerEntity);
            DeactivateEffigyLocator(performer, locator);
            return false;
        }

        locator.PinpointerEntity = pinpointerEntity;

        if (TryComp<PinpointerComponent>(pinpointerEntity, out var pinpointer))
        {
            _pinpointer.SetTarget(pinpointerEntity, effigy, pinpointer);
            _pinpointer.SetActive(pinpointerEntity, true, pinpointer);
        }

        return true;
    }

    /// <summary>
    /// Delete the locator pinpointer and remove the temporary hand.
    /// </summary>
    /// <param name="performer">The Slasher currently using the locator.</param>
    /// <param name="locator">The Slasher's locator component.</param>
    private void DeactivateEffigyLocator(EntityUid performer, SlasherEffigyLocatorComponent locator)
    {
        if (locator.PinpointerEntity is { } pinpointer && Exists(pinpointer))
            QueueDel(pinpointer);

        locator.PinpointerEntity = null;

        if (!TryComp<HandsComponent>(performer, out var hands)
            || !_hands.TryGetHand((performer, hands), locator.HandId, out _))
            return;

        _hands.RemoveHand((performer, hands), locator.HandId);
    }

    /// <summary>
    /// Clean up the locator when its component shuts down.
    /// </summary>
    /// <param name="ent">The entity losing locator state.</param>
    /// <param name="args">The component shutdown event.</param>
    private void OnLocatorShutdown(Entity<SlasherEffigyLocatorComponent> ent, ref ComponentShutdown args)
    {
        DeactivateEffigyLocator(ent.Owner, ent.Comp);

        if (ent.Comp.ActionEntity is { } trackedAction
            && TryComp<SlasherRoleComponent>(ent.Owner, out var role))
        {
            role.ActionEntities.Remove(trackedAction);
        }

        ent.Comp.ActionEntity = null;
    }
}