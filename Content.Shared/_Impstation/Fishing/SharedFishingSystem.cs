using System;
using System.Numerics;
using Content.Shared.Actions;
using Content.Shared.Buckle.Components;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Movement.Events;
using Content.Shared.Physics;
using Content.Shared.Projectiles;
using Content.Shared.Stacks;
using Content.Shared.Weapons.Misc;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Toolshed.TypeParsers;

namespace Content.Shared._Impstation.Fishing;

//Imp : Basically a copy of GrapplingGunSystem
public abstract class SharedFishingRodSystem : EntitySystem
{
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly SharedJointSystem _joints = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public const string GrapplingJoint = "grappling";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CanWeightlessMoveEvent>(OnWeightlessMove);
        SubscribeAllEvent<RequestGrapplingReelMessage>(OnGrapplingReel);

        SubscribeLocalEvent<FishingRodComponent, GunShotEvent>(OnGrapplingShot);
        SubscribeLocalEvent<FishingRodComponent, ActivateInWorldEvent>(OnGunActivate);
        SubscribeLocalEvent<FishingRodComponent, HandDeselectedEvent>(OnGrapplingDeselected);

        SubscribeLocalEvent<FishingProjectileComponent, ProjectileEmbedEvent>(OnGrappleCollide);
        SubscribeLocalEvent<FishingProjectileComponent, JointRemovedEvent>(OnGrappleJointRemoved);
        SubscribeLocalEvent<FishingProjectileComponent, RemoveEmbedEvent>(OnRemoveEmbed);
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        #region chumming
        var queryBait = EntityQueryEnumerator<FishingBaitComponent>();

        //For each bait
        while (queryBait.MoveNext(out var uid, out var baitComp))
        {
            //Get the bait's transform component
            if (TryComp<TransformComponent>(uid, out var transformComp))
            {
                //Check if it's spaced
                if (transformComp.GridUid == null)
                {
                    //Increase the fishing timer
                    baitComp.Timer += frameTime;

                    //Make sure we're above minimum spawn time
                    if (baitComp.Timer > baitComp.MinimumSpawnTime)
                    {
                        //Chance a catch
                        if (_robustRandom.Prob(Math.Min(frameTime / baitComp.AverageSpawnTime, 1.0f)))
                        {
                            //Select the catch
                            float totalProb = 0; //Sum of probabilities
                            float targetProb = _robustRandom.GetRandom().NextFloat(); //Random number between 0 & 1
                            foreach (KeyValuePair<string, float> potentialCatch in baitComp.Catches)
                            {
                                totalProb += potentialCatch.Value;
                                if (totalProb > targetProb)
                                {
                                    //Remove a stack from the bait
                                    if (TryComp<StackComponent>(uid, out var stackComp))
                                    {
                                        if (_stack.Use(uid, 1))
                                        {
                                            //Reset the timer
                                            baitComp.Timer = 0;

                                            //Spawn a catch
                                            EntityManager.SpawnEntity(potentialCatch.Key, Transform(uid).Coordinates);

                                            //Stop looping
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

        }
        #endregion

        #region reeling
        var queryRod = EntityQueryEnumerator<FishingRodComponent>();

        while (queryRod.MoveNext(out var uid, out var grappling))
        {
            if (!grappling.Reeling)
            {
                if (Timing.IsFirstTimePredicted)
                {
                    // Just in case.
                    grappling.Stream = _audio.Stop(grappling.Stream);
                }

                continue;
            }

            if (!TryComp<JointComponent>(uid, out var jointComp) ||
                !jointComp.GetJoints.TryGetValue(GrapplingJoint, out var joint) ||
                joint is not DistanceJoint distance)
            {
                SetReeling(uid, grappling, false, null);
                continue;
            }

            // TODO: This should be on engine.
            distance.MaxLength = MathF.Max(distance.MinLength, distance.MaxLength - grappling.ReelRate * frameTime);
            distance.Length = MathF.Min(distance.MaxLength, distance.Length);

            _physics.WakeBody(joint.BodyAUid);
            _physics.WakeBody(joint.BodyBUid);

            if (jointComp.Relay != null)
            {
                _physics.WakeBody(jointComp.Relay.Value);
            }

            Dirty(uid, jointComp);

            if (distance.MaxLength.Equals(distance.MinLength))
            {
                SetReeling(uid, grappling, false, null);
            }
        }
        #endregion
    }
    private void OnGrappleJointRemoved(EntityUid uid, FishingProjectileComponent component, JointRemovedEvent args)
    {
        if (_netManager.IsServer)
            QueueDel(uid);
    }

    private void OnGrapplingShot(EntityUid uid, FishingRodComponent component, ref GunShotEvent args)
    {
        foreach (var (shotUid, _) in args.Ammo)
        {
            //Add tackle damage modifier
            #region tackle damage modifier
            //Get the tackle container
            if (_container.TryGetContainer(uid, "tackle", out BaseContainer? container))
            {
                //Foreach in case we ever implement multiple tackle at once I guess
                //Also saves me from checking if the tackle exists
                foreach (var tackle in container.ContainedEntities)
                {
                    //Get the tackle component from the entity inside the tackle container
                    if (TryComp<FishingTackleComponent>(tackle, out var tackleComp))
                    {
                        //Get the projectile component from the shot fired
                        if (TryComp<ProjectileComponent>(shotUid, out var projectileComp))
                        {
                            //Add the damage modifier to the shot
                            foreach (KeyValuePair<string, FixedPoint.FixedPoint2> bonusDamage in tackleComp.Damage.DamageDict)
                            {
                                if (projectileComp.Damage.DamageDict.ContainsKey(bonusDamage.Key))
                                {
                                    projectileComp.Damage.DamageDict[bonusDamage.Key] += bonusDamage.Value;
                                }
                                else
                                {
                                    projectileComp.Damage.DamageDict.Add(bonusDamage.Key, bonusDamage.Value);
                                }
                            }
                        }
                    }
                }
            }
            #endregion

            if (!HasComp<FishingProjectileComponent>(shotUid))
                continue;
            //todo: this doesn't actually support multigrapple
            // At least show the visuals.
            component.Projectile = shotUid.Value;
            Dirty(uid, component);
            var visuals = EnsureComp<JointVisualsComponent>(shotUid.Value);
            visuals.Sprite = component.RopeSprite;
            visuals.OffsetA = new Vector2(0f, 0.5f);
            visuals.Target = GetNetEntity(uid);
            Dirty(shotUid.Value, visuals);
        }

        TryComp<AppearanceComponent>(uid, out var appearance);
        _appearance.SetData(uid, SharedTetherGunSystem.TetherVisualsStatus.Key, false, appearance);
        Dirty(uid, component);
    }

    private void OnGrapplingDeselected(EntityUid uid, FishingRodComponent component, HandDeselectedEvent args)
    {
        SetReeling(uid, component, false, args.User);
    }

    private void OnGrapplingReel(RequestGrapplingReelMessage msg, EntitySessionEventArgs args)
    {
        var player = args.SenderSession.AttachedEntity;
        if (!TryComp<HandsComponent>(player, out var hands) ||
            !TryComp<FishingRodComponent>(hands.ActiveHandEntity, out var grappling))
        {
            return;
        }

        if (msg.Reeling &&
            (!TryComp<CombatModeComponent>(player, out var combatMode) ||
             !combatMode.IsInCombatMode))
        {
            return;
        }

        SetReeling(hands.ActiveHandEntity.Value, grappling, msg.Reeling, player.Value);
    }

    private void OnWeightlessMove(ref CanWeightlessMoveEvent ev)
    {
        if (ev.CanMove || !TryComp<JointRelayTargetComponent>(ev.Uid, out var relayComp))
            return;

        foreach (var relay in relayComp.Relayed)
        {
            if (TryComp<JointComponent>(relay, out var jointRelay) && jointRelay.GetJoints.ContainsKey(GrapplingJoint))
            {
                ev.CanMove = true;
                return;
            }
        }
    }

    private void OnGunActivate(EntityUid uid, FishingRodComponent component, ActivateInWorldEvent args)
    {
        if (!Timing.IsFirstTimePredicted || args.Handled || !args.Complex || component.Projectile is not {} projectile)
            return;

        _audio.PlayPredicted(component.CycleSound, uid, args.User);
        _appearance.SetData(uid, SharedTetherGunSystem.TetherVisualsStatus.Key, true);

        if (_netManager.IsServer)
            QueueDel(projectile);

        component.Projectile = null;
        SetReeling(uid, component, false, args.User);
        _gun.ChangeBasicEntityAmmoCount(uid,  1);

        _joints.RemoveJoint(uid, GrapplingJoint);

        args.Handled = true;
    }

    private void SetReeling(EntityUid uid, FishingRodComponent component, bool value, EntityUid? user)
    {
        if (component.Reeling == value)
            return;

        if (value)
        {
            if (Timing.IsFirstTimePredicted)
                component.Stream = _audio.PlayPredicted(component.ReelSound, uid, user)?.Entity;
        }
        else
        {
            if (Timing.IsFirstTimePredicted)
            {
                component.Stream = _audio.Stop(component.Stream);
            }
        }

        component.Reeling = value;
        Dirty(uid, component);
    }
    private void OnGrappleCollide(EntityUid uid, FishingProjectileComponent component, ref ProjectileEmbedEvent args)
    {
        if (!Timing.IsFirstTimePredicted)
            return;

        //joint between the embedded and the weapon 
        var jointComp = EnsureComp<JointComponent>(args.Weapon);
        var joint = _joints.CreateDistanceJoint(args.Weapon, args.Embedded, anchorA: new Vector2(0f, 0.5f), id: GrapplingJoint);
        joint.MaxLength = joint.Length + 0.2f;
        joint.Stiffness = 1f;
        joint.MinLength = component.JointLength;
        // Setting velocity directly for mob movement fucks this so need to make them aware of it.
        // joint.Breakpoint = 4000f;
        Dirty(args.Weapon, jointComp);
    }

    private void OnRemoveEmbed(EntityUid uid, FishingProjectileComponent component, RemoveEmbedEvent args)
    {
        if (TryComp<EmbeddableProjectileComponent>(uid, out var projectile))
        {
            if (projectile.EmbeddedIntoUid != null)
            {
                _joints.ClearJoints(projectile.EmbeddedIntoUid.Value);
            }
        }
    }

    [Serializable, NetSerializable]
    protected sealed class RequestGrapplingReelMessage : EntityEventArgs
    {
        public bool Reeling;

        public RequestGrapplingReelMessage(bool reeling)
        {
            Reeling = reeling;
        }
    }
}
