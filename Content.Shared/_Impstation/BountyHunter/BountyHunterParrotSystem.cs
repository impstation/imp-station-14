using System.Diagnostics.CodeAnalysis;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.CombatMode;
using Robust.Shared.Timing;

namespace Content.Shared._Impstation.BountyHunter;

public sealed class PacificationSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BountyHunterParrotComponent, BeforeThrowEvent>(OnBeforeThrow);
        SubscribeLocalEvent<BountyHunterParrotComponent, AttackAttemptEvent>(OnAttackAttempt);
        SubscribeLocalEvent<BountyHunterParrotComponent, ShotAttemptedEvent>(OnShootAttempt);
    }
    private void OnShootAttempt(Entity<BountyHunterParrotComponent> ent, ref ShotAttemptedEvent args)
    {
        // Disallow firing guns in all cases.
        args.Cancel();
    }

    private void OnAttackAttempt(EntityUid uid, BountyHunterParrotComponent component, AttackAttemptEvent args)
    {
        // Disallow attacking in all cases.
        args.Cancel();
    }
    private void OnBeforeThrow(Entity<BountyHunterParrotComponent> ent, ref BeforeThrowEvent args)
    {
        // No throwing, either.
        args.Cancelled = true;
    }
}
