using Content.Server._Impstation.StationEvents.Components;
using Content.Server.StationEvents.Events;
using Content.Shared.EntityTable;
using Content.Shared.GameTicking.Components;

namespace Content.Server._Impstation.StationEvents.Events;

public sealed class RandomContinuousSpawnRule : StationEventSystem<RandomContinuousSpawnRuleComponent>
{
    [Dependency] private readonly EntityTableSystem _entityTable = default!;

    protected override void ActiveTick(EntityUid uid, RandomContinuousSpawnRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        if (Timing.CurTime < component.TimeUntilNextSpawn)
            return;

        var total = component.Amount.Next(RobustRandom);
        for (var i = 0; i < total; i++)
        {
            if (!TryFindRandomTile(out _, out _, out _, out var coords))
                continue;

            foreach (var proto in _entityTable.GetSpawns(component.Table))
            {
                Sawmill.Info($"Spawning {proto} at {coords}");
                Spawn(proto, coords);
            }
        }

        component.TimeUntilNextSpawn = Timing.CurTime + TimeSpan.FromSeconds(component.TimeBetweenSpawn.Next(RobustRandom));
    }
}
