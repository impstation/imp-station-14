using Content.Server.GameTicking.Rules;
using Content.Server.Ghost;
using Content.Server.Ghost.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;
using Content.Shared.Light.Components;

namespace Content.Server._Impstation.Slasher.Events;

/// <summary>
/// Triggers ghost boo interactions on a randomized set of station lights and spooky speakers.
/// </summary>
public sealed class SlasherGameRuleGhostBooSystem : SlasherPulseGameRuleSystem<SlasherGameRuleGhostBooComponent>
{
    [Dependency] private readonly GhostSystem _ghost = default!;

    /// <summary>
    /// Collects eligible station targets and runs ghost boo effects up to the configured cap.
    /// </summary>
    /// <param name="uid">Rule entity UID.</param>
    /// <param name="component">Rule configuration component.</param>
    /// <param name="gameRule">Base game-rule component.</param>
    /// <param name="args">Rule start event data.</param>
    protected override void Started(EntityUid uid, SlasherGameRuleGhostBooComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryGetPulseStation(out var chosenStation))
            return;

        var candidates = new HashSet<EntityUid>();

        var lightQuery = EntityQueryEnumerator<PoweredLightComponent, TransformComponent>();
        while (lightQuery.MoveNext(out var lightUid, out _, out var xform))
        {
            if (CompOrNull<StationMemberComponent>(xform.GridUid)?.Station != chosenStation)
                continue;

            candidates.Add(lightUid);
        }

        var speakerQuery = EntityQueryEnumerator<SpookySpeakerComponent, TransformComponent>();
        while (speakerQuery.MoveNext(out var speakerUid, out _, out var xform))
        {
            if (CompOrNull<StationMemberComponent>(xform.GridUid)?.Station != chosenStation)
                continue;

            candidates.Add(speakerUid);
        }

        if (candidates.Count == 0)
            return;

        var shuffled = new List<EntityUid>(candidates);
        RobustRandom.Shuffle(shuffled);

        var targetCount = Math.Clamp(component.MaxTargets, 1, shuffled.Count);
        for (var i = 0; i < targetCount; i++)
        {
            _ghost.DoGhostBooEvent(shuffled[i]);
        }
    }
}
