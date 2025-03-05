
using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared._Impstation.Fishing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FishingBaitComponent : Component
{
    /// <summary>
    /// Time since last catch
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Timer = 0;

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
    /// That means if the total of the floats is less than 1 theres a chance of catching nothing
    /// And if the total is above 1 then some probabilities might just not get rolled ever
    /// So try to keep it adding up to 1 for good practise
    /// Also probably should order this by rarity to simplify the math
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, float> Catches = new Dictionary<string, float>
    {
        {"MobCarp", 0.59f},
        {"ClothingShoesBootsSalvage", 0.25f},
        {"MobCarpMagic", 0.05f},
        {"MobCarpHolo", 0.05f},
        {"MobShark", 0.05f},
        {"MobDragonDungeon", 0.01f}
    };
}
