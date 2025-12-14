using System.Numerics;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
// start ee throwing
using Content.Shared._EE.Projectiles;
using Content.Shared._EE.Throwing;
using Content.Shared.Damage.Components;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Shared.Timing;
// end ee throwing

namespace Content.Shared.Projectiles;

public abstract partial class SharedProjectileSystem : EntitySystem
{
    public const string ProjectileFixture = "projectile";

    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!; // imp
    [Dependency] private readonly SharedPopupSystem _popup = default!; // imp

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ProjectileComponent, PreventCollideEvent>(PreventCollision);
        SubscribeLocalEvent<EmbeddableProjectileComponent, ProjectileHitEvent>(OnEmbedProjectileHit);
        SubscribeLocalEvent<EmbeddableProjectileComponent, ThrowDoHitEvent>(OnEmbedThrowDoHit);
        SubscribeLocalEvent<EmbeddableProjectileComponent, ActivateInWorldEvent>(OnEmbedActivate);
        SubscribeLocalEvent<EmbeddableProjectileComponent, RemoveEmbeddedProjectileEvent>(OnEmbedRemove);
        SubscribeLocalEvent<EmbeddableProjectileComponent, ComponentShutdown>(OnEmbeddableCompShutdown);
        SubscribeLocalEvent<EmbeddableProjectileComponent, ExaminedEvent>(OnExamined); // ee add

        SubscribeLocalEvent<EmbeddedContainerComponent, EntityTerminatingEvent>(OnEmbeddableTermination);
    }

    // ee: add update. things can fall out of you.
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<EmbeddableProjectileComponent>();
        var curTime = _timing.CurTime;

        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.AutoRemoveTime == null || comp.AutoRemoveTime > curTime)
                continue;

            if (comp.Target is { } targetUid)
                _popup.PopupClient(Loc.GetString("throwing-embed-falloff", ("item", uid)), targetUid, targetUid);

            EmbedDetach(uid, comp);
        }
    }


    private void OnEmbedActivate(Entity<EmbeddableProjectileComponent> embeddable, ref ActivateInWorldEvent args)
    {
        // Unremovable embeddables moment
        if (embeddable.Comp.RemovalTime == null)
            return;

        if (args.Handled || !args.Complex || !TryComp<PhysicsComponent>(embeddable, out var physics) ||
            physics.BodyType != BodyType.Static)
            return;

        args.Handled = true;

        // ee start
        if (embeddable.Comp.Target is { } targetUid)
            _popup.PopupClient(Loc.GetString("throwing-embed-remove-alert-owner", ("item", embeddable), ("other", args.User)),
                args.User, targetUid);
        // ee end

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            args.User,
            embeddable.Comp.RemovalTime.Value,
            new RemoveEmbeddedProjectileEvent(),
            eventTarget: embeddable,
            target: embeddable)
        // ee add
        {
            DistanceThreshold = SharedInteractionSystem.InteractionRange,
            BreakOnMove = true,
            NeedHand = true,
        });
        // ee end
    }

    private void OnEmbedRemove(Entity<EmbeddableProjectileComponent> embeddable, ref RemoveEmbeddedProjectileEvent args)
    {
        if (args.Cancelled)
            return;

        EmbedDetach(embeddable, embeddable.Comp, args.User);

        // try place it in the user's hand
        _hands.TryPickupAnyHand(args.User, embeddable);
    }

    private void OnEmbeddableCompShutdown(Entity<EmbeddableProjectileComponent> embeddable, ref ComponentShutdown arg)
    {
        EmbedDetach(embeddable, embeddable.Comp);
    }

    private void OnEmbedThrowDoHit(Entity<EmbeddableProjectileComponent> embeddable, ref ThrowDoHitEvent args)
    {
        // ee add pacifism check
        if (HasComp<PacifiedComponent>(args.Component.Thrower)
            && HasComp<MobStateComponent>(args.Target)
            && TryComp<DamageOtherOnHitComponent>(embeddable, out var damage)
            && damage.Damage.AnyPositive())
            return;

        if (!embeddable.Comp.EmbedOnThrow ||
            HasComp<ThrownItemImmuneComponent>(args.Target)) // ee add
            return;

        EmbedAttach(embeddable, args.Target, null, embeddable.Comp);
    }

    private void OnEmbedProjectileHit(Entity<EmbeddableProjectileComponent> embeddable, ref ProjectileHitEvent args)
    {
        EmbedAttach(embeddable, args.Target, args.Shooter, embeddable.Comp);

        // Raise a specific event for projectiles.
        if (TryComp(embeddable, out ProjectileComponent? projectile))
        {
            var ev = new ProjectileEmbedEvent(projectile.Shooter!.Value, projectile.Weapon!.Value, args.Target);
            RaiseLocalEvent(embeddable, ref ev);
        }
    }

    private void EmbedAttach(EntityUid uid, EntityUid target, EntityUid? user, EmbeddableProjectileComponent component)
    {
        // imp edit - who the fuck uses TryComp and just prays it returns something. are you fucking kidding me?
        if (!TryComp<PhysicsComponent>(uid, out var physics))
            return;

        _physics.SetLinearVelocity(uid, Vector2.Zero, body: physics);
        _physics.SetBodyType(uid, BodyType.Static, body: physics);
        var xform = Transform(uid);
        _transform.SetParent(uid, xform, target);

        if (component.Offset != Vector2.Zero)
        {
            var rotation = xform.LocalRotation;
            if (TryComp<ThrowingAngleComponent>(uid, out var throwingAngleComp))
                rotation += throwingAngleComp.Angle;
            _transform.SetLocalPosition(uid, xform.LocalPosition + rotation.RotateVec(component.Offset), xform);
        }

        _audio.PlayPredicted(component.Sound, uid, null);
        component.EmbeddedIntoUid = target;
        var ev = new EmbedEvent(user, target);
        RaiseLocalEvent(uid, ref ev);

        // ee add embedded shit
        var embeddedEv = new EmbeddedEvent(user, uid);
        RaiseLocalEvent(target, ref embeddedEv);

        if (component.AutoRemoveDuration != 0)
            component.AutoRemoveTime = _timing.CurTime + TimeSpan.FromSeconds(component.AutoRemoveDuration);

        component.Target = target;
        // End ee edits

        Dirty(uid, component);

        EnsureComp<EmbeddedContainerComponent>(target, out var embeddedContainer);

        //Assert that this entity not embed
        DebugTools.AssertEqual(embeddedContainer.EmbeddedObjects.Contains(uid), false);

        embeddedContainer.EmbeddedObjects.Add(uid);
    }

    public void EmbedDetach(EntityUid uid, EmbeddableProjectileComponent? component, EntityUid? user = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.EmbeddedIntoUid == null)
            return; // the entity is not embedded, so do nothing

        // ee add auto fall out
        component.AutoRemoveTime = null;
        component.Target = null;

        var ev = new RemoveEmbedEvent(user);
        RaiseLocalEvent(uid, ref ev);
        // ee end

        if (TryComp<EmbeddedContainerComponent>(component.EmbeddedIntoUid.Value, out var embeddedContainer))
        {
            embeddedContainer.EmbeddedObjects.Remove(uid);
            Dirty(component.EmbeddedIntoUid.Value, embeddedContainer);
            if (embeddedContainer.EmbeddedObjects.Count == 0)
                RemCompDeferred<EmbeddedContainerComponent>(component.EmbeddedIntoUid.Value);
        }

        if (component.DeleteOnRemove)
        {
            PredictedQueueDel(uid);
            return;
        }

        var xform = Transform(uid);
        if (TerminatingOrDeleted(xform.GridUid) && TerminatingOrDeleted(xform.MapUid))
            return;
        // imp edit - who the fuck uses TryComp and just prays it returns something. are you fucking kidding me?
        if (!TryComp<PhysicsComponent>(uid, out var physics))
            return;
        _physics.SetBodyType(uid, BodyType.Dynamic, body: physics, xform: xform);
        _transform.AttachToGridOrMap(uid, xform);
        component.EmbeddedIntoUid = null;
        Dirty(uid, component);

        // Reset whether the projectile has damaged anything if it successfully was removed
        if (TryComp<ProjectileComponent>(uid, out var projectile))
        {
            projectile.Shooter = null;
            projectile.Weapon = null;
            projectile.ProjectileSpent = false;

            Dirty(uid, projectile);
        }

        if (user != null)
        {
            // Land it just coz uhhh yeah
            var landEv = new LandEvent(user, true);
            RaiseLocalEvent(uid, ref landEv);
        }

        _physics.WakeBody(uid, body: physics);
    }

    private void OnEmbeddableTermination(Entity<EmbeddedContainerComponent> container, ref EntityTerminatingEvent args)
    {
        RemoveEmbeddedChildren(container);
    }

    // TODO: CLEAN THIS UP
    /// <summary>
    /// Imp: Unembeds all child entities on a given entity.
    /// </summary>
    public void RemoveEmbeddedChildren(EntityUid uid)
    {
        var enumerator = Transform(uid).ChildEnumerator;

        while (enumerator.MoveNext(out var child))
        {
            if (TryComp<EmbeddableProjectileComponent>(child, out var embed))
                EmbedDetach(child, embed);
        }
    }
    private void PreventCollision(EntityUid uid, ProjectileComponent component, ref PreventCollideEvent args)
    {
        if (component.IgnoreShooter && (args.OtherEntity == component.Shooter || args.OtherEntity == component.Weapon))
        {
            args.Cancelled = true;
        }
    }

    public void SetShooter(EntityUid id, ProjectileComponent component, EntityUid shooterId)
    {
        if (component.Shooter == shooterId)
            return;

        component.Shooter = shooterId;
        Dirty(id, component);
    }

    // ee add
    private void OnExamined(EntityUid uid, EmbeddableProjectileComponent component, ExaminedEvent args)
    {
        var message = new FormattedMessage();

        if (component.Target is { } target)
        {
            var targetIdentity = Identity.Entity(target, EntityManager);
            message.AddMarkupOrThrow(Loc.GetString("throwing-examine-embedded", ("embedded", uid), ("target", targetIdentity)));
            message.PushNewline();
        }

        if (component.EmbedOnThrow)
        {
            var isHarmful = TryComp<EmbedPassiveDamageComponent>(uid, out var passiveDamage) && passiveDamage.Damage.AnyPositive();
            var loc = isHarmful
                ? "damage-examine-embeddable-harmful"
                : "damage-examine-embeddable";
            message.AddMarkupOrThrow(Loc.GetString(loc));
            message.PushNewline();
        }

        args.AddMessage(message);
    }

    [Serializable, NetSerializable]
    private sealed partial class RemoveEmbeddedProjectileEvent : DoAfterEvent
    {
        public override DoAfterEvent Clone() => this;
    }
}

[Serializable, NetSerializable]
public sealed class ImpactEffectEvent : EntityEventArgs
{
    public string Prototype;
    public NetCoordinates Coordinates;

    public ImpactEffectEvent(string prototype, NetCoordinates coordinates)
    {
        Prototype = prototype;
        Coordinates = coordinates;
    }
}

/// <summary>
/// Raised when an entity is just about to be hit with a projectile but can reflect it
/// </summary>
[ByRefEvent]
public record struct ProjectileReflectAttemptEvent(EntityUid ProjUid, ProjectileComponent Component, bool Cancelled) : IInventoryRelayEvent
{
    SlotFlags IInventoryRelayEvent.TargetSlots => SlotFlags.WITHOUT_POCKET;
}

/// <summary>
/// Raised when a projectile hits an entity
/// </summary>
[ByRefEvent]
public record struct ProjectileHitEvent(DamageSpecifier Damage, EntityUid Target, EntityUid? Shooter = null);
