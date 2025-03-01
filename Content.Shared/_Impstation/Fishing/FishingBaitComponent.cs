
using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared._Impstation.Fishing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FishingBaitComponent : Component
{
    /// <summary>
    /// Minimum spawn time before anything can bite in second
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MinimumSpawnTime = 5;

    /// <summary>
    /// Average spawn time for something to bite the bait in second
    /// In practise, for a spawn time of x, we will attempt to spawn an item at a 1/x chance
    /// </summary>
    [DataField, AutoNetworkedField]
    public float AverageSpawnTime = 30;

    /// <summary>
    /// List of things that can be caught and the probability of them being caught
    /// In practise, everytime something is caught, a dice is rolled and compared against the accumulated floats below
    /// That means if the total of the floats is less than 100 theres a chance of catching nothing
    /// And if the total is above 100 then some probabilities just won't be rolled ever
    /// So try to keep it adding up to 100 for good practise
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, float> Catches = new Dictionary<string, float>
        {
            {"ClothingShoesBootsSalvage", 50},
            {"BaseMobCarp", 49},
            {"MobDragonDungeon", 1}
        };
}
