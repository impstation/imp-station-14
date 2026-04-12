using Content.Server._Impstation.GameTicking.Rules;
using Content.Server.GameTicking;
using Content.Shared._Impstation.Slasher.Components;
using Content.Shared._Impstation.Slasher.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Impstation.Slasher;

/// <summary>
/// Handles Slasher effigy pulse chance checks, weighted pulse selection,
/// anti-repeat behavior, and game rule execution.
/// </summary>
public sealed class SlasherEffigyPulseSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SlasherRuleSystem _rule = default!;

    /// <summary>
    /// Attempts to fire one or two pulse effects using the effigy's configured pulse chance and current insertion progress.
    /// </summary>
    /// <param name="ent">Effigy entity and component data.</param>
    public void TriggerHidePulse(Entity<SlasherEffigyComponent> ent)
    {
        if (!_random.Prob(ent.Comp.PulseChance))
            return;

        var pulseCount = 1;
        if (_rule.TryGetActiveRule(out var rule) && rule.Comp.TargetInsertions > 0)
        {
            var progress = (float) rule.Comp.FragmentInsertions / rule.Comp.TargetInsertions;
            if (progress > ent.Comp.DoublePulseProgressThreshold)
                pulseCount = 2;
        }

        for (var i = 0; i < pulseCount; i++)
            TriggerPulse(ent);
    }

    /// <summary>
    /// Picks a weighted random pulse effect (skipping the last used one) and starts it as a game rule.
    /// </summary>
    /// <param name="ent">Effigy entity and component data.</param>
    private void TriggerPulse(Entity<SlasherEffigyComponent> ent)
    {
        if (!_proto.TryIndex(ent.Comp.PulseWeightsProto, out PulseWeightsPrototype? weightsProto))
            return;

        // Ignore disabled entries so zero-weight pulses behave as fully unavailable.
        var candidates = new List<(string Id, int Weight)>();
        foreach (var entry in weightsProto.Weights)
        {
            if (entry.Weight > 0)
                candidates.Add((entry.ProtoId, entry.Weight));
        }

        // Anti-repeat only applies when there is still at least one alternative candidate left.
        if (candidates.Count > 1 && ent.Comp.LastPulseEffect != null)
            candidates.RemoveAll(c => c.Id == ent.Comp.LastPulseEffect);

        if (candidates.Count == 0)
            return;

        // Roll once against the summed weights, then walk the table cumulatively.
        var total = 0;
        foreach (var (_, weight) in candidates)
            total += weight;

        var roll = _random.Next(total);
        var cumulative = 0;
        var chosen = candidates[0].Id;
        foreach (var (id, weight) in candidates)
        {
            cumulative += weight;
            if (roll < cumulative)
            {
                chosen = id;
                break;
            }
        }

        ent.Comp.LastPulseEffect = chosen;
        _gameTicker.StartGameRule(chosen);
    }
}
