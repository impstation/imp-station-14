using Content.Server._Impstation.StationEvents.Events;
using Content.Shared.Destructible.Thresholds;
using Content.Shared.EntityTable.EntitySelectors;

namespace Content.Server._Impstation.StationEvents.Components;

[RegisterComponent, Access(typeof(RandomContinuousSpawnRule))]
public sealed partial class RandomContinuousSpawnRuleComponent : Component
{
    [DataField(required: true)]
    public EntityTableSelector Table = default!;

    [DataField]
    public MinMax Amount = new(1, 1);

    [DataField]
    public MinMax TimeBetweenSpawn = new(1, 1);

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TimeUntilNextSpawn;
}
