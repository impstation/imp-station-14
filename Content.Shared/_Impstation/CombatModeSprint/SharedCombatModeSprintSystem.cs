using Content.Shared.CombatMode;
using Content.Shared.Damage.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Shared._Impstation.CombatModeSprint;

/// <summary>
/// Applies combat-mode sprint modifiers and optional collision damage for entities with <see cref="CombatModeSprintComponent"/>.
/// </summary>
public abstract class SharedCombatModeSprintSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] private readonly DamageOnHighSpeedImpactSystem _impact = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    /// <summary>
    /// Registers combat-mode sprint handlers.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CombatModeSprintComponent, CombatModeChangedEvent>(OnCombatModeChanged);
        SubscribeLocalEvent<CombatModeSprintComponent, ComponentStartup>(OnHandleState);
        SubscribeLocalEvent<CombatModeSprintComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
    }

    /// <summary>
    /// Refreshes movement modifiers when the component starts.
    /// </summary>
    /// <param name="ent">Entity and sprint component data.</param>
    /// <param name="args">Startup event data.</param>
    private void OnHandleState(Entity<CombatModeSprintComponent> ent, ref ComponentStartup args)
    {
        _movementSpeed.RefreshMovementSpeedModifiers(ent);
    }

    /// <summary>
    /// Updates sprint state and shows entry or exit popups when combat mode changes.
    /// </summary>
    /// <param name="ent">Entity and sprint component data.</param>
    /// <param name="args">Combat mode change event data.</param>
    private void OnCombatModeChanged(Entity<CombatModeSprintComponent> ent, ref CombatModeChangedEvent args)
    {
        _movementSpeed.RefreshMovementSpeedModifiers(ent);
        if (ent.Comp.BeginCombatMessage != null && args.Enabled)
            _popup.PopupEntity(Loc.GetString(ent.Comp.BeginCombatMessage, ("name", Identity.Entity(ent, EntityManager))), ent, Filter.PvsExcept(ent), true);
        if (ent.Comp.EndCombatMessage != null && !args.Enabled)
            _popup.PopupEntity(Loc.GetString(ent.Comp.EndCombatMessage, ("name", Identity.Entity(ent, EntityManager))), ent, Filter.PvsExcept(ent), true);
    }

    /// <summary>
    /// Applies movement-speed and collision-damage settings for the entity's current combat-mode state.
    /// </summary>
    /// <param name="ent">Entity and sprint component data.</param>
    /// <param name="args">Movement speed modifier aggregation event.</param>
    private void OnRefreshMovespeed(Entity<CombatModeSprintComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (_combatMode.IsInCombatMode(ent))
        {
            args.ModifySpeed(ent.Comp.SprintCoefficient);
            if (ent.Comp.DoImpactDamage)
                _impact.ChangeCollide(ent, ent.Comp.MinimumSpeed, ent.Comp.StunSeconds, ent.Comp.DamageCooldown, ent.Comp.SpeedDamage);
        }
        else
        {
            args.ModifySpeed(1f);
            if (ent.Comp.DoImpactDamage)
                _impact.ChangeCollide(ent, ent.Comp.DefaultMinimumSpeed, ent.Comp.DefaultStunSeconds, ent.Comp.DefaultDamageCooldown, ent.Comp.DefaultSpeedDamage);
        }
    }
}

