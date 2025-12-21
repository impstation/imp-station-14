using Content.Server.Administration.Logs;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Camera;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Effects;
using Content.Shared.Mobs.Components;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Content.Shared._EE.Damage.Components; // EE THROWING
using Content.Shared.CombatMode.Pacification; // EE THROWING
using Content.Shared.Popups; // EE THROWING
using Content.Shared.Projectiles; // EE THROWING
using Content.Shared.Weapons.Melee; // EE THROWING
using Robust.Shared.Physics.Systems; // EE THROWING
using Robust.Shared.Prototypes; // EE THROWING

namespace Content.Server.Damage.Systems;

public sealed class DamageOtherOnHitSystem : SharedDamageOtherOnHitSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly GunSystem _guns = default!;
    [Dependency] private readonly Shared.Damage.Systems.DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _sharedCameraRecoil = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!; // EE THROWING
    [Dependency] private readonly MeleeSoundSystem _meleeSound = default!; // EE THROWING
    [Dependency] private readonly SharedPhysicsSystem _physics = default!; // EE THROWING
    [Dependency] private readonly SharedPopupSystem _popup = default!; // EE THROWING
    [Dependency] private readonly StaminaSystem _stamina = default!; // EE THROWING
    [Dependency] private readonly ThrownItemSystem _thrownItem = default!; // EE THROWING

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageOtherOnHitComponent, ThrowDoHitEvent>(OnDoHit);
        SubscribeLocalEvent<StaminaComponent, BeforeThrowEvent>(OnBeforeThrow); // EE THROWING
    }

    private void OnDoHit(EntityUid uid, DamageOtherOnHitComponent component, ThrowDoHitEvent args)
    {
        if (TerminatingOrDeleted(args.Target))
            return;

        // EE START
        if (HasComp<DamageOtherOnHitImmuneComponent>(args.Target) || !TryComp<PhysicsComponent>(uid, out var physics))
            return;

        if (HasComp<PacifiedComponent>(args.Component.Thrower)
            && HasComp<MobStateComponent>(args.Target)
            && component.Damage.AnyPositive())
            return;

        var isEmbedded = TryComp<EmbeddableProjectileComponent>(uid, out var embed) && embed.EmbeddedIntoUid != null;

        // Ignore thrown items that are too slow, as long as the projectile is not embedded
        if (!isEmbedded && physics.LinearVelocity.LengthSquared() < component.MinVelocity)
            return;
        // EE END

        var dmg = _damageable.ChangeDamage(args.Target, component.Damage * _damageable.UniversalThrownDamageModifier, component.IgnoreResistances, origin: args.Component.Thrower);

        // Log damage only for mobs. Useful for when people throw spears at each other, but also avoids log-spam when explosions send glass shards flying.
        if (HasComp<MobStateComponent>(args.Target))
            _adminLogger.Add(LogType.ThrowHit, $"{ToPrettyString(args.Target):target} received {dmg.GetTotal():damage} damage from collision");

        if (!dmg.Empty)
        {
            _color.RaiseEffect(Color.Red, [args.Target], Filter.Pvs(args.Target, entityManager: EntityManager));
        }

        // EE START: use MeleeSoundSystem for more modularity
        //_guns.PlayImpactSound(args.Target, dmg, null, false);
        _meleeSound.PlayHitSound(args.Target, null,
            SharedMeleeWeaponSystem.GetHighestDamageSound(dmg, _protoMan),
            null, component.HitSound, component.NoDamageSound);
        // EE END
        if (TryComp<PhysicsComponent>(uid, out var body) && body.LinearVelocity.LengthSquared() > 0f)
        {
            var direction = body.LinearVelocity.Normalized();
            _sharedCameraRecoil.KickCamera(args.Target, direction);
        }

        // EE START
        if (HasComp<StaminaComponent>(args.Target) && TryComp<StaminaDamageOnHitComponent>(uid, out var stamina) &&
            !HasComp<StaminaDamageOnCollideComponent>(uid)) // imp add
            _stamina.TakeStaminaDamage(args.Target, stamina.Damage, source: uid, sound: stamina.Sound);

        // TODO: If more stuff touches this then handle it after.
        _thrownItem.LandComponent(args.Thrown, args.Component, physics, false);

        // bounce!
        if (!HasComp<EmbeddableProjectileComponent>(args.Thrown))
        {
            var newVelocity = physics.LinearVelocity;
            newVelocity.X = -newVelocity.X / 4;
            newVelocity.Y = -newVelocity.Y / 4;
            _physics.SetLinearVelocity(uid, newVelocity, body: physics);
        }

        if (TryComp<ThrownItemComponent>(uid, out var thrown))
            thrown.HitQuantity += 1;
        // EE END
    }

    // EE THROWING
    private void OnBeforeThrow(EntityUid uid, StaminaComponent component, ref BeforeThrowEvent args)
    {
        if (!TryComp<DamageOtherOnHitComponent>(args.ItemUid, out var damage))
            return;

        if (component.CritThreshold - component.StaminaDamage <= damage.StaminaCost)
        {
            args.Cancelled = true;
            _popup.PopupEntity(Loc.GetString("throw-no-stamina", ("item", args.ItemUid)), uid, uid);
            return;
        }
    }
}
