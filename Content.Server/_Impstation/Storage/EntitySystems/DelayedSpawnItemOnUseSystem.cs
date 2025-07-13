using Content.Server._Impstation.Storage.Components;
using Content.Server.Administration.Logs;
using Content.Server.Cargo.Systems;
using Content.Server.Storage.Components;
using Content.Shared._Impstation.Storage.Events;
using Content.Shared.Cargo;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;
using static Content.Shared.Storage.EntitySpawnCollection;
namespace Content.Server._Impstation.Storage.EntitySystems;

public sealed class DelayedSpawnItemOnUseSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly SharedHandsSystem _hands = default!;
        [Dependency] private readonly PricingSystem _pricing = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DelayedSpawnItemsOnUseComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<DelayedSpawnItemsOnUseComponent, PriceCalculationEvent>(CalculatePrice, before: new[] { typeof(PricingSystem) });
            SubscribeLocalEvent<DelayedSpawnItemsOnUseComponent, DelayedSpawnItemOnUseDoAfterEvent>(OnDoAfter);
        }

        private void CalculatePrice(EntityUid uid, DelayedSpawnItemsOnUseComponent component, ref PriceCalculationEvent args)
        {
            var ungrouped = CollectOrGroups(component.Items, out var orGroups);

            foreach (var entry in ungrouped)
            {
                var protUid = Spawn(entry.PrototypeId, MapCoordinates.Nullspace);

                // Calculate the average price of the possible spawned items
                args.Price += _pricing.GetPrice(protUid) * entry.SpawnProbability * entry.GetAmount(getAverage: true);

                EntityManager.DeleteEntity(protUid);
            }

            foreach (var group in orGroups)
            {
                foreach (var entry in group.Entries)
                {
                    var protUid = Spawn(entry.PrototypeId, MapCoordinates.Nullspace);

                    // Calculate the average price of the possible spawned items
                    args.Price += _pricing.GetPrice(protUid) *
                                  (entry.SpawnProbability / group.CumulativeProbability) *
                                  entry.GetAmount(getAverage: true);

                    EntityManager.DeleteEntity(protUid);
                }
            }

            args.Handled = true;
        }

        private void OnUseInHand(EntityUid uid, DelayedSpawnItemsOnUseComponent component, UseInHandEvent args)
        {
            if (args.Handled)
                return;

            // If starting with zero or less uses, this component is a no-op
            if (component.Uses <= 0)
                return;

            if (component.PopUpEnabled)
                _popupSystem.PopupEntity(component.PopUpMessage,args.User,args.User);

            var doargs = new DoAfterArgs(EntityManager, args.User, component.Delay, new DelayedSpawnItemOnUseDoAfterEvent(),uid)
            {
                NeedHand = component.NeedHand,
                BreakOnHandChange = component.BreakOnHandChange,
                BreakOnDropItem = component.BreakOnDropItem,
                BreakOnDamage = component.BreakOnDamage,
                BreakOnMove = component.BreakOnMove,
                RequireCanInteract = component.RequireCanInteract,
                BlockDuplicate = component.BlockDuplicate,
                CancelDuplicate = component.CancelDuplicate,
                AttemptFrequency = AttemptFrequency.StartAndEnd
            };
            _doAfter.TryStartDoAfter(doargs);
            args.Handled = true;
        }

        private void OnDoAfter(EntityUid uid,
            DelayedSpawnItemsOnUseComponent component,
            DelayedSpawnItemOnUseDoAfterEvent args)
        {
            if (args.Handled||args.Cancelled)
                return;

            var coords = Transform(args.User).Coordinates;
            var spawnEntities = GetSpawns(component.Items, _random);
            EntityUid? entityToPlaceInHands = null;

            foreach (var proto in spawnEntities)
            {
                entityToPlaceInHands = Spawn(proto, coords);
                _adminLogger.Add(LogType.EntitySpawn, LogImpact.Low, $"{ToPrettyString(args.User)} used {ToPrettyString(uid)} which spawned {ToPrettyString(entityToPlaceInHands.Value)}");
            }

            // The entity is often deleted, so play the sound at its position rather than parenting
            if (component.Sound != null)
                _audio.PlayPvs(component.Sound, coords);

            component.Uses--;

            // Delete entity only if component was successfully used
            if (component.Uses <= 0)
            {
                // Don't delete the entity in the event bus, so we queue it for deletion.
                // We need the free hand for the new item, so we send it to nullspace.
                _transform.DetachEntity(uid, Transform(uid));
                QueueDel(uid);
            }

            if (entityToPlaceInHands != null)
                _hands.PickupOrDrop(args.User, entityToPlaceInHands.Value);

            args.Handled = true;
        }
}
