using Content.Shared.Actions;
using Content.Shared.Dataset;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.StrangeMoods;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedStrangeMoodsSystem))]
public sealed partial class StrangeMoodsComponent : Component
{
    /// <summary>
    /// The shared mood prototype that this entity will look to follow.
    /// </summary>
    [DataField("shared"), AutoNetworkedField]
    public ProtoId<SharedMoodPrototype>? SharedMoodPrototype;

    /// <summary>
    /// The dataset prototypes that moods will be pulled from, as well as the amount of moods to be given.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public Dictionary<ProtoId<DatasetPrototype>, int> Datasets = [];

    /// <summary>
    /// The shared moods that this entity follows.
    /// If null, the entity will not follow any shared moods.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SharedMood? SharedMood;

    /// <summary>
    /// The non-shared moods that are active.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<StrangeMood> Moods = [];

    /// <summary>
    /// Notification sound played if your moods change.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? MoodsChangedSound = new SoundPathSpecifier("/Audio/_Impstation/StrangeMoods/moods_changed.ogg");

    /// <summary>
    /// The color of the mood change chat notification.
    /// </summary>
    [DataField("alertColor"), AutoNetworkedField]
    public Color MoodsChangedColor = Color.Orange;

    /// <summary>
    /// The action used to view moods.
    /// </summary>
    [DataField]
    public EntProtoId ActionViewMoods = "ActionViewMoods";

    [DataField(serverOnly: true)]
    public EntityUid? Action;
}

public sealed partial class ToggleMoodsScreenEvent : InstantActionEvent;

[NetSerializable, Serializable]
public enum StrangeMoodsUiKey : byte
{
    Key
}

/// <summary>
/// BUI state to tell the client what the shared moods are.
/// </summary>
[Serializable, NetSerializable]
public sealed class StrangeMoodsBuiState(List<StrangeMood> sharedMoods) : BoundUserInterfaceState
{
    public readonly List<StrangeMood> SharedMoods = sharedMoods;
}
