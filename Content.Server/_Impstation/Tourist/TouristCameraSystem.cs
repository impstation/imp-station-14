using System.Linq;
using Content.Server.Flash.Components;
using Content.Shared.Flash.Components;
using Content.Server.Light.EntitySystems;
using Content.Server.Popups;
using Content.Server.Stunnable;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Flash;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Tag;
using Content.Shared.Traits.Assorted;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.StatusEffect;
using Content.Shared.Examine;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Random;
using InventoryComponent = Content.Shared.Inventory.InventoryComponent;
using Content.Shared.Throwing;
using Robust.Shared.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.Mind.Components;
using Content.Shared._Impstation.Tourist;
using Content.Shared._Impstation.Tourist.Components;
using Content.Server._Impstation.Tourist.Components;
using Content.Server.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Shared.Mind;
using Robust.Shared.Containers;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Hands.Components;

namespace Content.Server._Impstation.Tourist
{
    internal sealed class TouristCameraSystem : SharedTouristCameraSystem
    {
        [Dependency] private readonly AppearanceSystem _appearance = default!;
        [Dependency] private readonly AudioSystem _audio = default!;
        [Dependency] private readonly SharedChargesSystem _sharedCharges = default!;
        [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly ExamineSystemShared _examine = default!;
        [Dependency] private readonly InventorySystem _inventory = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly StunSystem _stun = default!;
        [Dependency] private readonly TagSystem _tag = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
        [Dependency] private readonly CodeConditionSystem _codeCondition = default!;
        [Dependency] private readonly SharedMindSystem _mind = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;
        [Dependency] private readonly SharedHandsSystem _hands = default!;

        private static readonly ProtoId<TagPrototype> TrashTag = "Trash";

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<TouristCameraComponent, UseInHandEvent>(OnFlashUseInHand, before: new[] { typeof(HandheldLightSystem) });
            SubscribeLocalEvent<TouristCameraComponent, TouristCameraDoAfterEvent>(OnDoAfter);
            SubscribeLocalEvent<InventoryComponent, FlashAttemptEvent>(OnInventoryFlashAttempt);
            SubscribeLocalEvent<FlashImmunityComponent, FlashAttemptEvent>(OnFlashImmunityFlashAttempt);
            SubscribeLocalEvent<PermanentBlindnessComponent, FlashAttemptEvent>(OnPermanentBlindnessFlashAttempt);
            SubscribeLocalEvent<TemporaryBlindnessComponent, FlashAttemptEvent>(OnTemporaryBlindnessFlashAttempt);
        }

        private void OnFlashUseInHand(EntityUid uid, TouristCameraComponent comp, UseInHandEvent args)
        {
            if (args.Handled)
                return;

            args.Handled = true;

            var doAfterArgs = new DoAfterArgs(EntityManager, args.User, comp.DoAfterDuration, new TouristCameraDoAfterEvent(), uid, target: uid)
            {
                BreakOnDamage = true,
                BreakOnMove = true,
                NeedHand = true
            };

            _doAfter.TryStartDoAfter(doAfterArgs);
        }

        private void OnDoAfter(EntityUid uid, TouristCameraComponent comp, TouristCameraDoAfterEvent args)
        {
            if (args.Cancelled || !UseFlash(uid, comp))
                return;

            args.Handled = true;
            FlashArea(uid, args.User, comp.Range, comp.AoeFlashDuration, comp.SlowTo, true, comp.Probability);
        }

        private bool UseFlash(EntityUid uid, TouristCameraComponent comp)
        {
            if (comp.Flashing)
                return false;

            _audio.PlayPvs(comp.Sound, uid);
            comp.Flashing = true;
            _appearance.SetData(uid, TouristCameraVisuals.Flashing, true);

            uid.SpawnTimer(400, () =>
            {
                _appearance.SetData(uid, TouristCameraVisuals.Flashing, false);
                comp.Flashing = false;
            });

            return true;
        }

        public void Flash(EntityUid target,
            EntityUid? user,
            EntityUid? used,
            float flashDuration,
            float slowTo,
            bool displayPopup = true,
            bool melee = false,
            TimeSpan? stunDuration = null)
        {
            var attempt = new FlashAttemptEvent(target, user, used);
            RaiseLocalEvent(target, attempt, true);

            if (attempt.Cancelled)
                return;

            // don't paralyze, slowdown or convert to rev if the target is immune to flashes
            if (!_statusEffectsSystem.TryAddStatusEffect<FlashedComponent>(target, FlashedKey, TimeSpan.FromSeconds(flashDuration / 1000f), true))
                return;

            if (stunDuration != null)
            {
                _stun.TryParalyze(target, stunDuration.Value, true);
            }
            else
            {
                _stun.TrySlowdown(target, TimeSpan.FromSeconds(flashDuration / 1000f), true,
                slowTo, slowTo);
            }

            if (displayPopup && user != null && target != user && Exists(user.Value))
            {
                _popup.PopupEntity(Loc.GetString("flash-component-user-blinds-you",
                    ("user", Identity.Entity(user.Value, EntityManager))), target, target);
            }

            if (melee)
            {
                var ev = new AfterFlashedEvent(target, user, used);
                if (user != null)
                    RaiseLocalEvent(user.Value, ref ev);
                if (used != null)
                    RaiseLocalEvent(used.Value, ref ev);
            }
        }

        public void FlashArea(Entity<TouristCameraComponent?> source, EntityUid? user, float range, float duration, float slowTo = 0.8f, bool displayPopup = false, float probability = 1f, SoundSpecifier? sound = null)
        {
            var transform = Transform(source);
            var mapPosition = _transform.GetMapCoordinates(transform);
            var statusEffectsQuery = GetEntityQuery<StatusEffectsComponent>();
            var damagedByFlashingQuery = GetEntityQuery<DamagedByFlashingComponent>();

            //if you're seeing this, I once again forgot the debug loggers

            foreach (var entity in _entityLookup.GetEntitiesInRange(transform.Coordinates, range))
            {
                // Check for entites in view
                // put damagedByFlashingComponent in the predicate because shadow anomalies block vision.
                if (!_examine.InRangeUnOccluded(entity, mapPosition, range, predicate: (e) => damagedByFlashingQuery.HasComponent(e)))
                    continue;

                //this will probably be moved to its own helper function to handle multiple objectives at once
                if (user != null && TryComp<TouristComponent>(user, out var tourist) && _mind.TryGetObjectiveComp<TouristPhotosConditionComponent>(user.Value, out var condition))
                {
                    var prototype = MetaData(entity).EntityPrototype;

                    //skip if we've already taken a photo of this entity
                    if (tourist.PhotographedEntities.Contains(entity))
                    {
                        Logger.Info($"Skipping because already stored.");
                        continue;
                    }

                    //skip if entity is in a container, but don't skip if it's being worn or held
                    if (!_inventory.TryGetContainingSlot(entity, out var slot) && !TryComp<HandsComponent>(Transform(entity).ParentUid, out _) && _container.IsEntityInContainer(entity))
                    {
                        Logger.Debug($"Skipping {entity} - it's in an inventory/container");
                        continue;
                    }

                    bool validTarget = false;

                    //check if the entity is a targeted prototype
                    if (condition?.TargetPrototypes != null && prototype?.ID is { } prototypeId && condition.TargetPrototypes.Contains(prototypeId))
                    {
                        Logger.Info($"{entity} is valid prototype");
                        validTarget = true;
                    }
                    /* commenting this out and coming back to it later because goddamn
                    //this is a bit of a monster, but check if the entity has a targeted job prototype
                    else if (condition.TargetJobs != null && TryComp<MindContainerComponent>(entity, out var mindContainer))
                    {
                        var mind = mindContainer.Mind;
                        if (mind != null && TryComp<MindComponent>(mind, out var mindComp))
                        {
                            foreach (var role in mindComp.MindRoles)
                            {
                                if (condition.TargetJobs.Contains(role.Prototype.ID))
                                {
                                    validTarget = true;
                                    break; // Exit early if any matching role is found
                                }
                            }
                        }
                    }
                    */


                    if (validTarget)
                    {
                        // save entity uid to the tourist component
                        tourist.PhotographedEntities.Add(entity);
                        Logger.Info($"Saving entity.");
                        // yay greentext!
                        if (_mind.TryGetObjectiveComp<TouristPhotosConditionComponent>(user.Value, out var obj))
                        {
                            Logger.Info($"Adding to greentext");
                            obj.Photos++;
                        }

                    }
                }


                if (!_random.Prob(probability))
                    continue;

                // Is the entity affected by the flash either through status effects or by taking damage?
                if (!statusEffectsQuery.HasComponent(entity) && !damagedByFlashingQuery.HasComponent(entity))
                    continue;

                // They shouldn't have flash removed in between right?
                Flash(entity, user, source, duration, slowTo, displayPopup);
            }

            _audio.PlayPvs(sound, source, AudioParams.Default.WithVolume(1f).WithMaxDistance(3f));
        }

        private void OnInventoryFlashAttempt(EntityUid uid, InventoryComponent component, FlashAttemptEvent args)
        {
            foreach (var slot in new[] { "head", "eyes", "mask" })
            {
                if (args.Cancelled)
                    break;
                if (_inventory.TryGetSlotEntity(uid, slot, out var item, component))
                    RaiseLocalEvent(item.Value, args, true);
            }
        }

        private void OnFlashImmunityFlashAttempt(EntityUid uid, FlashImmunityComponent component, FlashAttemptEvent args)
        {
            if (component.Enabled)
                args.Cancel();
        }

        private void OnPermanentBlindnessFlashAttempt(EntityUid uid, PermanentBlindnessComponent component, FlashAttemptEvent args)
        {
            // check for total blindness
            if (component.Blindness == 0)
                args.Cancel();
        }

        private void OnTemporaryBlindnessFlashAttempt(EntityUid uid, TemporaryBlindnessComponent component, FlashAttemptEvent args)
        {
            args.Cancel();
        }
    }

    /// <summary>
    ///     Called before a flash is used to check if the attempt is cancelled by blindness, items or FlashImmunityComponent.
    ///     Raised on the target hit by the flash, the user of the flash and the flash used.
    /// </summary>
    public sealed class FlashAttemptEvent : CancellableEntityEventArgs
    {
        public readonly EntityUid Target;
        public readonly EntityUid? User;
        public readonly EntityUid? Used;

        public FlashAttemptEvent(EntityUid target, EntityUid? user, EntityUid? used)
        {
            Target = target;
            User = user;
            Used = used;
        }
    }
    /// <summary>
    ///     Called after a flash is used via melee on another person to check for rev conversion.
    ///     Raised on the target hit by the flash, the user of the flash and the flash used.
    /// </summary>
    [ByRefEvent]
    public readonly struct AfterFlashedEvent
    {
        public readonly EntityUid Target;
        public readonly EntityUid? User;
        public readonly EntityUid? Used;

        public AfterFlashedEvent(EntityUid target, EntityUid? user, EntityUid? used)
        {
            Target = target;
            User = user;
            Used = used;
        }
    }
}
