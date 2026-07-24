using Content.Server._Impstation.Slasher.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Containers;

namespace Content.Server._Impstation.Slasher;

/// <summary>
/// Enforces the Slasher chainsaw's committed use pattern, fueled damage bonus,
/// and shutdown behavior when it leaves the user's committed grip.
/// </summary>
public sealed class SlasherChainsawSystem : EntitySystem
{
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly ItemToggleSystem _itemToggle = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    /// <summary>
    /// Subscribes held-visual, combat, and auto-shutdown hooks for slasher chainsaws.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlasherChainsawComponent, GotEquippedHandEvent>(OnEquippedHand);
        SubscribeLocalEvent<SlasherChainsawComponent, GetMeleeAttackRateEvent>(OnGetMeleeAttackRate);
        SubscribeLocalEvent<SlasherChainsawComponent, GetMeleeDamageEvent>(OnGetMeleeDamage);
        SubscribeLocalEvent<SlasherChainsawComponent, GotUnequippedHandEvent>(OnUnequippedHand);
        SubscribeLocalEvent<SlasherChainsawComponent, DroppedEvent>(OnDropped);
        SubscribeLocalEvent<SlasherChainsawComponent, EntGotInsertedIntoContainerMessage>(OnInsertedIntoContainer);
    }

    /// <summary>
    /// Burns welding fuel while activated so a full tank lasts thirty seconds.
    /// </summary>
    /// <param name="frameTime">Elapsed frame time in seconds.</param>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SlasherChainsawComponent, ItemToggleComponent, SolutionContainerManagerComponent>();
        while (query.MoveNext(out var uid, out var chainsaw, out var toggle, out var manager))
        {
            if (!toggle.Activated)
                continue;

            BurnFuel((uid, chainsaw), manager, frameTime);
        }
    }

    /// <summary>
    /// Applies the wielded held-prefix as soon as the chainsaw enters a hand.
    /// </summary>
    /// <param name="ent">Chainsaw entity and component data.</param>
    /// <param name="args">Hand-equip event data.</param>
    private void OnEquippedHand(Entity<SlasherChainsawComponent> ent, ref GotEquippedHandEvent args)
    {
        _item.SetHeldPrefix(ent.Owner, ent.Comp.HeldPrefix);
    }

    /// <summary>
    /// Swaps between the chainsaw's dormant and running attack rates without interfering with other modifiers.
    /// </summary>
    /// <param name="ent">Chainsaw entity and component data.</param>
    /// <param name="args">Attack-rate query for the current swing.</param>
    private void OnGetMeleeAttackRate(Entity<SlasherChainsawComponent> ent, ref GetMeleeAttackRateEvent args)
    {
        if (!TryComp<ItemToggleComponent>(ent, out var toggle) || !toggle.Activated)
            return;

        args.Rate += ent.Comp.ActiveAttackRate - ent.Comp.InactiveAttackRate;
    }

    /// <summary>
    /// Applies the chainsaw's extra fueled damage only while it is active and still has welding fuel.
    /// </summary>
    /// <param name="ent">Chainsaw entity and component data.</param>
    /// <param name="args">Damage query for the current swing.</param>
    private void OnGetMeleeDamage(Entity<SlasherChainsawComponent> ent, ref GetMeleeDamageEvent args)
    {
        if (!TryComp<ItemToggleComponent>(ent, out var toggle) || !toggle.Activated)
            return;

        if (!TryComp<SolutionContainerManagerComponent>(ent, out var manager))
            return;

        if (!HasFuel((ent.Owner, ent.Comp), manager))
            return;

        args.Damage *= ent.Comp.FueledDamageMultiplier;
    }

    /// <summary>
    /// Clears the wielded held-prefix and turns the chainsaw off as soon as it leaves a hand.
    /// </summary>
    /// <param name="ent">Chainsaw entity and component data.</param>
    /// <param name="args">Hand-unequip data.</param>
    private void OnUnequippedHand(Entity<SlasherChainsawComponent> ent, ref GotUnequippedHandEvent args)
    {
        _item.SetHeldPrefix(ent.Owner, null);
        TryDeactivate(ent.Owner);
    }

    /// <summary>
    /// Turns the chainsaw off when it is dropped.
    /// </summary>
    /// <param name="ent">Chainsaw entity and component data.</param>
    /// <param name="args">Drop-event data.</param>
    private void OnDropped(Entity<SlasherChainsawComponent> ent, ref DroppedEvent args)
    {
        _item.SetHeldPrefix(ent.Owner, null);
        TryDeactivate(ent.Owner);
    }

    /// <summary>
    /// Turns the chainsaw off when it is inserted into a container.
    /// </summary>
    /// <param name="ent">Chainsaw entity and component data.</param>
    /// <param name="args">Container-insert data.</param>
    private void OnInsertedIntoContainer(Entity<SlasherChainsawComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        _item.SetHeldPrefix(ent.Owner, null);
        TryDeactivate(ent.Owner);
    }

    /// <summary>
    /// Safely deactivates the chainsaw when it is currently running.
    /// </summary>
    /// <param name="uid">Chainsaw entity UID.</param>
    private void TryDeactivate(EntityUid uid)
    {
        if (!TryComp<ItemToggleComponent>(uid, out var toggle) || !toggle.Activated)
            return;

        _itemToggle.TryDeactivate((uid, toggle), predicted: false, showPopup: false);
    }

    /// <summary>
    /// Removes active fuel consumption from the chainsaw, preserving fractional progress between frames.
    /// </summary>
    /// <param name="ent">Chainsaw entity and component data.</param>
    /// <param name="manager">Solution container manager that owns the fuel tank.</param>
    /// <param name="frameTime">Elapsed frame time in seconds.</param>
    private void BurnFuel(Entity<SlasherChainsawComponent> ent, SolutionContainerManagerComponent manager, float frameTime)
    {
        if (!TryGetFuelSolution((ent.Owner, ent.Comp), manager, out var fuelSolution))
            return;

        ent.Comp.FuelConsumptionRemainder += ent.Comp.FuelConsumptionPerSecond * frameTime;
        var hundredthsToConsume = (int)(ent.Comp.FuelConsumptionRemainder * 100f);
        if (hundredthsToConsume <= 0)
            return;

        ent.Comp.FuelConsumptionRemainder -= hundredthsToConsume / 100f;
        var amount = FixedPoint2.FromHundredths(hundredthsToConsume);
        _solution.RemoveReagent(fuelSolution, ent.Comp.FuelReagent, amount);
    }

    /// <summary>
    /// Checks whether the chainsaw still has any welding fuel left for its bonus damage.
    /// </summary>
    /// <param name="ent">Chainsaw entity and component data.</param>
    /// <param name="manager">Solution container manager that owns the fuel tank.</param>
    /// <returns>True when the configured fuel reagent is present.</returns>
    private bool HasFuel(Entity<SlasherChainsawComponent> ent, SolutionContainerManagerComponent manager)
    {
        if (!TryGetFuelSolution(ent, manager, out var fuelSolution))
            return false;

        return fuelSolution.Comp.Solution.ContainsReagent(ent.Comp.FuelReagent, null);
    }

    /// <summary>
    /// Resolves the chainsaw's configured internal fuel solution.
    /// </summary>
    /// <param name="ent">Chainsaw entity and component data.</param>
    /// <param name="manager">Solution container manager that owns the fuel tank.</param>
    /// <param name="fuelSolution">Resolved fuel solution, if present.</param>
    /// <returns>True when the configured fuel solution exists.</returns>
    private bool TryGetFuelSolution(
        Entity<SlasherChainsawComponent> ent,
        SolutionContainerManagerComponent manager,
        out Entity<SolutionComponent> fuelSolution)
    {
        fuelSolution = default;
        if (!_solution.TryGetSolution((ent.Owner, manager), ent.Comp.FuelSolution, out Entity<SolutionComponent>? resolved))
            return false;

        if (resolved is null)
            return false;

        fuelSolution = resolved.Value;
        return true;
    }
}