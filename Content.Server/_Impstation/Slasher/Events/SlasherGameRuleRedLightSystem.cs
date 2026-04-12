using Content.Server.GameTicking.Rules;
using Content.Server.Light.EntitySystems;
using Content.Shared.GameTicking.Components;
using Content.Shared.Light.Components;
using Content.Shared.Station.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Slasher.Events;

/// <summary>
/// Replaces bulbs in a randomized subset of station powered lights with red bulbs.
/// </summary>
public sealed class SlasherGameRuleRedLightSystem : SlasherPulseGameRuleSystem<SlasherGameRuleRedLightComponent>
{
    [Dependency] private readonly PoweredLightSystem _poweredLight = default!;

    /// <summary>
    /// Selects station lights, swaps bulbs, and cleans up ejected originals.
    /// </summary>
    /// <param name="uid">Rule entity UID.</param>
    /// <param name="component">Rule configuration component.</param>
    /// <param name="gameRule">Base game-rule component.</param>
    /// <param name="args">Rule start event data.</param>
    protected override void Started(EntityUid uid, SlasherGameRuleRedLightComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryGetPulseStation(out var chosenStation))
            return;

        var lights = new List<EntityUid>();
        var query = AllEntityQuery<PoweredLightComponent, TransformComponent>();
        while (query.MoveNext(out var lightUid, out _, out var xform))
        {
            if (CompOrNull<StationMemberComponent>(xform.GridUid)?.Station != chosenStation)
                continue;

            lights.Add(lightUid);
        }

        if (lights.Count == 0)
            return;

        RobustRandom.Shuffle(lights);
        var replaceCount = RobustRandom.Next(component.MinLights, component.MaxLights + 1);
        replaceCount = Math.Clamp(replaceCount, 1, lights.Count);

        for (var i = 0; i < replaceCount; i++)
        {
            var light = lights[i];
            if (!TryComp<PoweredLightComponent>(light, out var lightComp))
                continue;

            var oldBulb = _poweredLight.GetBulb(light, lightComp);
            if (oldBulb is { } old)
                _poweredLight.EjectBulb(light, null, lightComp);

            var newBulb = Spawn(component.BulbPrototype, Transform(light).Coordinates);
            if (!_poweredLight.InsertBulb(light, newBulb, lightComp))
                Del(newBulb);

            if (oldBulb is { } droppedOldBulb && !Deleted(droppedOldBulb))
                Del(droppedOldBulb);
        }
    }
}
