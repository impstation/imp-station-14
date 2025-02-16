using Content.Server.Power.Components;
using Robust.Shared.Timing;
using Content.Server.Power.EntitySystems;
using Content.Shared.Atmos;
using Robust.Server.GameObjects;
using Content.Server.Atmos.EntitySystems;

namespace Content.Server._Impstation.Power
{
    public sealed class ItemMinerSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
        [Dependency] private readonly TransformSystem _transform = default!;
        [Dependency] private readonly BatterySystem _battery = default!;
        [Dependency] private readonly IGameTiming _timing = default!;

        /// <summary>
        /// Per-tick cache
        /// </summary>
        private readonly List<GasMixture> _environments = new();

        public override void Update(float frameTime)
        {
            var query = EntityQueryEnumerator<ItemMinerComponent, PowerConsumerComponent, BatteryComponent, TransformComponent>();

            while (query.MoveNext(out var entity, out var component, out var networkLoad, out var battery, out var transform))
            {
                //If the miner isnt anchored or if it isnt receiving power then nothing happens
                if (!transform.Anchored || networkLoad.NetworkLoad.ReceivingPower <= 0)
                    continue;

                //Otherwise it starts producing heat (heat code taken from latheheatproducingsystem that hyperlathes use)
                #region heat production
                if (component.NextSecond == default)
                {
                    component.NextSecond = _timing.CurTime;
                }
                if (_timing.CurTime > component.NextSecond)
                {
                    component.NextSecond += TimeSpan.FromSeconds(1);

                    var position = _transform.GetGridTilePositionOrDefault((entity, transform));
                    _environments.Clear();

                    if (_atmosphere.GetTileMixture(transform.GridUid, transform.MapUid, position, true) is { } tileMix)
                        _environments.Add(tileMix);

                    if (transform.GridUid != null)
                    {
                        var enumerator = _atmosphere.GetAdjacentTileMixtures(transform.GridUid.Value, position, false, true);
                        while (enumerator.MoveNext(out var mix))
                        {
                            _environments.Add(mix);
                        }
                    }

                    if (_environments.Count > 0)
                    {
                        var heatPerTile = component.EnergyPerSecond / _environments.Count;
                        foreach (var env in _environments)
                        {
                            _atmosphere.AddHeat(env, heatPerTile);
                        }
                    }
                }
                #endregion

                //We add the power it drains from the grid to its battery
                var newCharge = battery.CurrentCharge + networkLoad.NetworkLoad.ReceivingPower;
                while (newCharge > component.ChargeThreashold)
                {
                    //And if we're above the threadshold the battery gets turned into the mined item
                    EntityManager.SpawnEntity(component.ItemConvertion, Transform(entity).Coordinates);
                    newCharge -= component.ChargeThreashold;
                }

                //And we update the battery's charge
                _battery.SetCharge(entity, newCharge, battery);
            }
        }
    }
}
