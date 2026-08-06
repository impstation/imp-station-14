using Content.Shared._Impstation.Heretic.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;

namespace Content.Shared._Impstation.Heretic.EntitySystems;

/// <summary>
/// Shared version of <see cref="MinionSystem"/> for prediction.
/// </summary>
public abstract class SharedMinionSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MinionComponent, AttackAttemptEvent>(OnAttackAttempt);
    }

    /// <summary>
    /// Called when a minion attempts to attack.
    /// </summary>
    /// <param name="ent">The minion's MinionComp, and its associated entit</param>
    /// <param name="ev"><see cref="AttackAttemptEvent"/></param>
    private void OnAttackAttempt(Entity<MinionComponent> ent, ref AttackAttemptEvent ev)
    {
        // No attacking your summoner.
        if (ent.Comp.BoundOwner != null && ev.Target == ent.Comp.BoundOwner)
        {
            _popup.PopupClient(Loc.GetString("heretic-minion-no-attack"), ent.Owner, ent.Owner, PopupType.MediumCaution);
            ev.Cancel();
        }

        // No attacking minions from the same master, either
        if (ent.Comp.BoundOwner != null && TryComp<MinionComponent>(ev.Target, out var targComp))
        {
            if (targComp.BoundOwner == ent.Comp.BoundOwner)
            {
                _popup.PopupClient(Loc.GetString("heretic-kin-no-attack"), ent.Owner, ent.Owner, PopupType.MediumCaution);
                ev.Cancel();
            }
        }
    }
}
