using Content.Shared.Explosion.Components;
using Content.Shared.Explosion.EntitySystems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Server._Impstation.Explosion;

public sealed class ExplodeOnMeleeHitSystem : EntitySystem
{
    [Dependency] private readonly SharedExplosionSystem _explosion = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ExplodeOnMeleeHitComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<ExplodeOnMeleeHitComponent, ThrowDoHitEvent>(OnThrowHit);
    }

    private void OnMeleeHit(Entity<ExplodeOnMeleeHitComponent> ent, ref MeleeHitEvent args)
    {
        if (args.HitEntities.Count == 0 || args.Handled)
            return;

        TryExplode(ent, args.User);
    }

    private void OnThrowHit(Entity<ExplodeOnMeleeHitComponent> ent, ref ThrowDoHitEvent args)
    {
        if (!ent.Comp.ExplodeOnThrow || args.Handled)
            return;

        TryExplode(ent, args.User);
    }

    private void TryExplode(Entity<ExplodeOnMeleeHitComponent> ent, EntityUid? user)
    {
        if (!TryComp<ExplosiveComponent>(ent, out var explosive))
            return;

        if (ent.Comp.ReplaceWith != null && !ent.Comp.HasSpawnedItem)
        {
            var coords = Transform(ent).Coordinates;
            var newItem = Spawn(ent.Comp.ReplaceWith, coords);

            if (user != null)
            {
                _hands.TryDrop(user.Value, ent);
                _hands.TryPickup(user.Value, newItem);
            }

            ent.Comp.HasSpawnedItem = true;
        }

        _explosion.TriggerExplosive(ent, explosive, user: user);
    }
}
