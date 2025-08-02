using Content.Server.Atmos.EntitySystems;
using Content.Server.Cargo.Systems;
using Content.Server.GameTicking.Events;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.Atmos.Prototypes;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.GalacticMarket;


public sealed class GalacticMarketSystem : EntitySystem
{

    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = null!;
    [Dependency] private readonly PricingSystem _pricing = null!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = null!;

    private EntityUid _marketEnt = EntityUid.Invalid;

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
        SubscribeLocalEvent<EntitySoldEvent>(OnEntitySold);
    }

    private void OnRoundStart(ref RoundStartingEvent ev)
    {
        //probably unnecessary but I like the safety
        var query = EntityQueryEnumerator<GalacticMarketComponent>();
        while (query.MoveNext(out var ent, out var comp))
        {
            QueueDel(ent);
        }

        _marketEnt = Spawn(null, MapCoordinates.Nullspace);
        AddComp<GalacticMarketComponent>(_marketEnt);
    }

    /// <summary>
    /// Updates the sell value dict with the amount a given entity sold for
    /// </summary>
    private void OnEntitySold(ref EntitySoldEvent ev)
    {
        var sellDict = new Dictionary<string, double>();

        foreach (var entity in ev.Sold)
        {
            var granularPrices =  new List<(EntityUid, double)>();
            var price = _pricing.GetPrice(entity, granularPrices: granularPrices);
            var protoid = MetaData(entity).EntityPrototype?.ID;

            //todo use granular prices

            //singling out gases as uniquely valuable for now, should really also account for solutions etc etc
            if (TryComp<GasTankComponent>(entity, out var gasTankComponent))
            {
                var gasPrice = _atmosphereSystem.GetPrice(gasTankComponent.Air);
                price -= gasPrice; //to split the value of the container from the value of the gas

                foreach (var gas in gasTankComponent.Air)
                {
                    var gasProto = _prototypeManager.Index<GasPrototype>(gas.gas.ToString());

                    sellDict[gasProto.Name] = (gas.moles / gasTankComponent.Air.TotalMoles) * gasPrice;
                }
            }

            if (TryComp<GasCanisterComponent>(entity, out var gasCanisterComponent))
            {
                var gasPrice = _atmosphereSystem.GetPrice(gasCanisterComponent.Air);
                price -= gasPrice; //to split the value of the container from the value of the gas

                foreach (var gas in gasCanisterComponent.Air)
                {
                    var gasProto = _prototypeManager.Index<GasPrototype>(gas.gas.ToString());

                    sellDict[gasProto.Name] = (gas.moles / gasCanisterComponent.Air.TotalMoles) * gasPrice; //todo make this not collide w/ the other one. idk if anything has a tank and canister comp at the same time so it doesn't really matter but eh
                }
            }

            if (protoid != null)
            {
                sellDict[protoid] = price;
            }
        }

        var spesosDict = GetGeneratedSpesos();

        foreach (var key in sellDict.Keys)
        {
            if (spesosDict.TryGetValue(key, out var spesos))
            {
                spesosDict[key] = spesos + sellDict[key];
            }
            else
            {
                spesosDict.Add(key, sellDict[key]);
            }
        }
    }

    //API
    public Dictionary<string, double> GetGeneratedSpesos()
    {
        var comp = Comp<GalacticMarketComponent>(_marketEnt);
        return comp.SpesosGenerated;
    }

    public float GetSellPriceMultForGas(int gas)
    {
        return GetSellPriceMultForPrototype(_prototypeManager.Index<GasPrototype>(gas.ToString()).Name);
    }

    /// <summary>
    /// get the sell price multiplier for a given prototype
    /// </summary>
    /// <param name="prototype">the ID of the prototype. if you're looking for a gas, it has to be a name instead.</param>
    /// <returns></returns>
    public float GetSellPriceMultForPrototype(string prototype)
    {
        if (_prototypeManager.TryIndex<PriceCurvePrototype>(prototype, out var curve))
        {
            return GetGeneratedSpesos().TryGetValue(prototype, out var spesos) ? curve.GetPriceMult(spesos) : curve.GetPriceMult(0);
        }
        else
        {
            var defaultCurve = _prototypeManager.Index<PriceCurvePrototype>("default");
            return GetGeneratedSpesos().TryGetValue(prototype, out var spesos) ? defaultCurve.GetPriceMult(spesos) : defaultCurve.GetPriceMult(0);
        }
    }
}
