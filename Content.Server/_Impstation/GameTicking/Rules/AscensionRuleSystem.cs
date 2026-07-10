using Content.Server.AlertLevel;
using Content.Server.GameTicking.Rules;
using Content.Server.Ghost;
using Content.Shared.GameTicking.Components;
using Content.Shared.Light.Components;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server._Impstation.GameTicking.Rules;

/// <summary>
///     manages events that happen on a heretic ascension, such as setting the alert level to cobalt
/// </summary>
public sealed class AscensionRuleSystem : GameRuleSystem<AscensionRuleComponent>
{
    [Dependency] private readonly AlertLevelSystem _alertLevelSystem = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void Started(EntityUid uid, AscensionRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        comp.TimeForCobaltEffects = comp.DelayForCobaltEffects + Timing.CurTime;

        // Flickers all powered lights on all maps, for scary effect
        var query = EntityQueryEnumerator<MapComponent>();
        while (query.MoveNext(out var mapUid, out var hereticComp))
        {
            var mapId = Transform(mapUid).MapID;

            var lightLookup = new HashSet<Entity<PoweredLightComponent>>();
            _entityLookup.GetEntitiesOnMap<PoweredLightComponent>(mapId, lightLookup);
            foreach (var light in lightLookup)
            {
                if (!_random.Prob(comp.LightFlickerChance))
                    continue;
                _ghost.DoGhostBooEvent(light);
            }
        }
    }

    protected override void ActiveTick(EntityUid uid, AscensionRuleComponent comp, GameRuleComponent gameRule, float frameTime)
    {
        if (comp.TimeForCobaltEffects <= Timing.CurTime && !comp.CobaltEffectsTriggered)
        {
            comp.CobaltEffectsTriggered = true;
            SetAlertLevelCobalt();
        }
    }

    private void SetAlertLevelCobalt()

    {
        if (!TryGetRandomStation(out var station))
            return;
        if (_alertLevelSystem.GetLevel(station.Value) == "cobalt") // Don't cobalt if already cobalt
            return;
        _alertLevelSystem.SetLevel(station.Value, "cobalt", true, true, true);
    }
}
