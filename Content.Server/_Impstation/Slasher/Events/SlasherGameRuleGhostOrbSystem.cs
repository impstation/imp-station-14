using System.Numerics;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.GameTicking.Rules;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Server._Impstation.Slasher.Events;

/// <summary>
/// Launches ghost orbs from randomly selected station vents during a Slasher pulse.
/// </summary>
public sealed class SlasherGameRuleGhostOrbSystem : SlasherPulseGameRuleSystem<SlasherGameRuleGhostOrbComponent>
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    /// <summary>
    /// Samples vents on the chosen station, spawns orbs, and imparts randomized launch velocity.
    /// </summary>
    /// <param name="uid">Rule entity UID.</param>
    /// <param name="component">Rule configuration component.</param>
    /// <param name="gameRule">Base game-rule component.</param>
    /// <param name="args">Rule start event data.</param>
    protected override void Started(EntityUid uid, SlasherGameRuleGhostOrbComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryGetPulseStation(out var chosenStation))
            return;

        var vents = new List<EntityUid>();
        var ventQuery = EntityQueryEnumerator<GasVentPumpComponent>();
        while (ventQuery.MoveNext(out var ventUid, out _))
        {
            if (!IsOnPulseStation(ventUid, chosenStation))
                continue;

            vents.Add(ventUid);
        }

        if (vents.Count == 0)
            return;

        RobustRandom.Shuffle(vents);

        var sampleCount = Math.Clamp(component.VentSampleCount, 1, vents.Count);
        var orbCount = Math.Clamp(component.OrbCount, 1, sampleCount);

        for (var i = 0; i < orbCount; i++)
        {
            var ventUid = vents[i];
            var orb = Spawn(component.OrbPrototype, Transform(ventUid).Coordinates);

            if (!TryComp<PhysicsComponent>(orb, out var physics))
                continue;

            var angle = RobustRandom.NextFloat() * MathF.Tau;
            var velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * component.OrbSpeed;
            _physics.SetLinearVelocity(orb, velocity, body: physics);
        }
    }
}
