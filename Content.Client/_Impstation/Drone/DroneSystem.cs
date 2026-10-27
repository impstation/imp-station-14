using Content.Shared._Impstation.Drone.Components;
using Content.Shared._Impstation.Drone;
using Content.Shared.Alert;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client._Impstation.Drone
{
    public sealed class DroneSystem : SharedDroneSystem
    {
        [Dependency] private readonly AlertsSystem _alerts = default!;
        [Dependency] private readonly SharedBatterySystem _battery = default!;
        [Dependency] private readonly IPlayerManager _player = default!;
        [Dependency] private readonly PowerCellSystem _powerCell = default!;
        [Dependency] private readonly IGameTiming _timing = default!;

        // How often to update the battery alert.
        // Also gets updated instantly when switching bodies or a battery is inserted or removed.
        private static readonly TimeSpan AlertUpdateDelay = TimeSpan.FromSeconds(0.5f);

        // Don't put this on the component because we only need to track the time for a single entity
        // and we don't want to TryComp it every single tick.
        private TimeSpan _nextAlertUpdate = TimeSpan.Zero;
        private EntityQuery<PowerCellSlotComponent> _slotQuery;
        private EntityQuery<DroneComponent> _droneQuery;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DroneComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<DroneComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

            _droneQuery = GetEntityQuery<DroneComponent>();
            _slotQuery = GetEntityQuery<PowerCellSlotComponent>();
        }

        private void OnPlayerAttached(Entity<DroneComponent> ent, ref LocalPlayerAttachedEvent args)
        {
            UpdateBatteryAlert((ent.Owner, ent.Comp, null));
        }

        private void OnPlayerDetached(Entity<DroneComponent> ent, ref LocalPlayerDetachedEvent args)
        {
            // Remove all drone related alerts.
            _alerts.ClearAlert(ent.Owner, ent.Comp.BatteryAlert);
            _alerts.ClearAlert(ent.Owner, ent.Comp.NoBatteryAlert);
        }

        private void UpdateBatteryAlert(Entity<DroneComponent, PowerCellSlotComponent?> ent)
        {
            if (!Resolve(ent, ref ent.Comp2, false))
                return;

            if (!_powerCell.TryGetBatteryFromSlot((ent.Owner, ent.Comp2), out var battery))
            {
                _alerts.ShowAlert(ent.Owner, ent.Comp1.NoBatteryAlert);
                return;
            }

            // Alert levels from 0 to 10.
            var chargeLevel = (short)MathF.Round(_battery.GetChargeLevel(battery.Value.AsNullable()) * 10f);

            // we make sure 0 only shows if they have absolutely no battery.
            // also account for floating point imprecision
            if (chargeLevel == 0 && _powerCell.HasDrawCharge((ent.Owner, null, ent.Comp2)))
            {
                chargeLevel = 1;
            }

            _alerts.ShowAlert(ent.Owner, ent.Comp1.BatteryAlert, chargeLevel);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            UpdateBattery(frameTime);
        }

        // Periodically update the charge indicator.
        // We do this with a client-side alert so that we don't have to network the charge level.
        public void UpdateBattery(float frameTime)
        {
            if (_player.LocalEntity is not { } localPlayer)
                return;

            var curTime = _timing.CurTime;

            if (curTime < _nextAlertUpdate)
                return;

            _nextAlertUpdate = curTime + AlertUpdateDelay;

            if (!_droneQuery.TryComp(localPlayer, out var drone) || !_slotQuery.TryComp(localPlayer, out var slot))
                return;

            UpdateBatteryAlert((localPlayer, drone, slot));
        }
    }
}