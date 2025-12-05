using System.Linq;
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
using Content.Shared.Movement.Systems;
using Content.Shared.Stunnable;

namespace Content.Server._Impstation.Tourist
{
    //This is mostly duplicated from FlashSystem, as AfterFlashedEvent won't run on entities that are flash immune or being held/worn by something flash immune.
    internal sealed class TouristCameraSystem : SharedTouristCameraSystem
    {
        [Dependency] private readonly AppearanceSystem _appearance = default!;
        [Dependency] private readonly AudioSystem _audio = default!;
        [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly ExamineSystemShared _examine = default!;
        [Dependency] private readonly InventorySystem _inventory = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
        [Dependency] private readonly SharedMindSystem _mind = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;
        [Dependency] private readonly MovementModStatusSystem _movementMod = default!;
        [Dependency] private readonly SharedStunSystem _stun = default!;

        private static readonly ProtoId<TagPrototype> TrashTag = "Trash";

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<TouristCameraComponent, TouristCameraDoAfterEvent>(OnDoAfter);
            SubscribeLocalEvent<InventoryComponent, TouristCameraFlashAttemptEvent>(OnInventoryTouristFlashAttempt);
            SubscribeLocalEvent<FlashImmunityComponent, TouristCameraFlashAttemptEvent>(OnFlashImmunityTouristFlashAttempt);
            SubscribeLocalEvent<PermanentBlindnessComponent, TouristCameraFlashAttemptEvent>(OnPermanentBlindnessTouristFlashAttempt);
            SubscribeLocalEvent<TemporaryBlindnessComponent, TouristCameraFlashAttemptEvent>(OnTemporaryBlindnessTouristFlashAttempt);
        }

        private void OnDoAfter(EntityUid uid, TouristCameraComponent comp, TouristCameraDoAfterEvent args)
        {
            if (args.Cancelled || !CameraFlash(uid, comp))
                return;

            args.Handled = true;
            TouristCameraFlashArea(uid, args.User, comp.Range, comp.AoeFlashDuration, comp.SlowTo, true, comp.Probability);
        }

        private bool CameraFlash(EntityUid uid, TouristCameraComponent comp)
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


        public void TouristCameraFlash(
            EntityUid target,
            EntityUid? user,
            EntityUid? used,
            TimeSpan flashDuration,
            float slowTo,
            bool displayPopup = true,
            TimeSpan? stunDuration = null)
        {
            var attempt = new TouristCameraFlashAttemptEvent(target, user, used);
            RaiseLocalEvent(target, attempt, true);

            if (attempt.Cancelled)
                return;

            // don't paralyze, slowdown or convert to rev if the target is immune to flashes
            if (!_statusEffectsSystem.TryAddStatusEffect<FlashedComponent>(target, FlashedKey, flashDuration, true))
                return;

            if (stunDuration != null)
                _stun.TryUpdateParalyzeDuration(target, stunDuration.Value);
            else
                _movementMod.TryUpdateMovementSpeedModDuration(target, MovementModStatusSystem.FlashSlowdown, flashDuration, slowTo);

            if (displayPopup && user != null && target != user && Exists(user.Value))
            {
                _popup.PopupEntity(Loc.GetString("flash-component-user-blinds-you",
                    ("user", Identity.Entity(user.Value, EntityManager))), target, target);
            }
        }

        public void TouristCameraFlashArea(Entity<TouristCameraComponent?> source, EntityUid? user, float range, TimeSpan duration, float slowTo = 0.8f, bool displayPopup = false, float probability = 1f, SoundSpecifier? sound = null)
        {
            var transform = Transform(source);
            var mapPosition = _transform.GetMapCoordinates(transform);
            var statusEffectsQuery = GetEntityQuery<StatusEffectsComponent>();
            var damagedByFlashingQuery = GetEntityQuery<DamagedByFlashingComponent>();
            var objectives = new List<TouristPhotosConditionComponent>();
            if (user != null)
            {
                objectives = GetPhotoObjectives(user.Value);
            }


            foreach (var entity in _entityLookup.GetEntitiesInRange(transform.Coordinates, range))
            {
                // Check for entites in view
                // put damagedByFlashingComponent in the predicate because shadow anomalies block vision.
                if (!_examine.InRangeUnOccluded(entity, mapPosition, range, predicate: (e) => damagedByFlashingQuery.HasComponent(e)))
                    continue;

                if (user != null && TryComp<TouristComponent>(user, out var tourist) && objectives != null)
                {
                    foreach (var objective in objectives)
                    {
                        CheckForPhotoTargets(user.Value, entity, tourist, objective);
                    }

                }

                if (!_random.Prob(probability))
                    continue;

                // Is the entity affected by the flash either through status effects or by taking damage?
                if (!statusEffectsQuery.HasComponent(entity) && !damagedByFlashingQuery.HasComponent(entity))
                    continue;

                // They shouldn't have flash removed in between right?
                TouristCameraFlash(entity, user, source, duration, slowTo, displayPopup);
            }

            _audio.PlayPvs(sound, source, AudioParams.Default.WithVolume(1f).WithMaxDistance(3f));
        }

        private List<TouristPhotosConditionComponent> GetPhotoObjectives(EntityUid user)
        {
            var objectives = new List<TouristPhotosConditionComponent>();

            if (!Exists(user) || !_mind.TryGetMind(user, out var mindId, out var mind))
                return objectives;

            foreach (var objectiveId in mind.AllObjectives)
            {
                if (TryComp<TouristPhotosConditionComponent>(objectiveId, out var condition))
                {
                    objectives.Add(condition);
                }
            }
            return objectives;
        }

        private void CheckForPhotoTargets(EntityUid user, EntityUid entity, TouristComponent tourist, TouristPhotosConditionComponent condition)
        {
            var prototype = MetaData(entity).EntityPrototype;

            //skip if we've already taken a photo of this entity
            if (tourist.PhotographedEntities.Contains(entity))
            {
                return;
            }

            //skip if entity is in a container, but don't skip if it's being worn or held
            if (!_inventory.TryGetContainingSlot(entity, out var slot) && !TryComp<HandsComponent>(Transform(entity).ParentUid, out _) && _container.IsEntityInContainer(entity))
            {
                return;
            }

            //check if the entity is a targeted prototype
            if (!(prototype?.ID is { } prototypeId && condition.TargetPrototypes.Contains(prototypeId)))
                return;
            // save entity uid to the tourist component, then add it to the greentext.
            tourist.PhotographedEntities.Add(entity);
            condition.Photos++;

        }

        private void OnInventoryTouristFlashAttempt(EntityUid uid, InventoryComponent component, TouristCameraFlashAttemptEvent args)
        {
            foreach (var slot in new[] { "head", "eyes", "mask" })
            {
                if (args.Cancelled)
                    break;
                if (_inventory.TryGetSlotEntity(uid, slot, out var item, component))
                    RaiseLocalEvent(item.Value, args, true);
            }
        }

        private void OnFlashImmunityTouristFlashAttempt(EntityUid uid, FlashImmunityComponent component, TouristCameraFlashAttemptEvent args)
        {
            if (component.Enabled)
                args.Cancel();
        }

        private void OnPermanentBlindnessTouristFlashAttempt(EntityUid uid, PermanentBlindnessComponent component, TouristCameraFlashAttemptEvent args)
        {
            // check for total blindness
            if (component.Blindness == 0)
                args.Cancel();
        }

        private void OnTemporaryBlindnessTouristFlashAttempt(EntityUid uid, TemporaryBlindnessComponent component, TouristCameraFlashAttemptEvent args)
        {
            args.Cancel();
        }
    }

    /// <summary>
    ///     Called before a flash is used to check if the attempt is cancelled by blindness, items or FlashImmunityComponent.
    ///     Raised on the target hit by the flash, the user of the flash and the flash used.
    /// </summary>
    public sealed class TouristCameraFlashAttemptEvent : CancellableEntityEventArgs
    {
        public readonly EntityUid Target;
        public readonly EntityUid? User;
        public readonly EntityUid? Used;

        public TouristCameraFlashAttemptEvent(EntityUid target, EntityUid? user, EntityUid? used)
        {
            Target = target;
            User = user;
            Used = used;
        }
    }
}
