using Content.Shared.Dataset;
using Content.Shared.Random;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Thaven;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ThavenMoodsComponent : Component
{
    /// <summary>
    /// The dataset from which wildcard moods are pulled.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<DatasetPrototype> WildcardDataset = "ThavenMoodsWildcard";

    /// <summary>
    /// The weighted dataset used to determine which dataset is used for the ion storm event.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<WeightedRandomPrototype> IonStormDataset = "RandomThavenMoodDataset";

    /// <summary>
    /// Whether to allow emagging to add a random wildcard mood.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanBeEmagged = true;

    /// <summary>
    /// Whether to allow ion storms to add a random mood.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IonStormable = true;

    /// <summary>
    /// The probability that an ion storm will remove a mood.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float IonStormRemoveChance = 0.25f;

    /// <summary>
    /// The probability that an ion storm will add a mood.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float IonStormAddChance = 0.25f;

    /// <summary>
    /// The maximum number of moods that an entity can be given by ion storms.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxIonMoods = 4;
}
