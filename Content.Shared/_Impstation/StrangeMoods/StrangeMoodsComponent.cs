using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.StrangeMoods;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedStrangeMoodsSystem))]
public sealed partial class StrangeMoodsComponent : Component
{
    /// <summary>
    /// The strange mood definition that this entity follows.
    /// </summary>
    [DataField("mood", required: true, readOnly: true)]
    public ProtoId<StrangeMoodDefinitionPrototype> StrangeMoodPrototype;

    [DataField, AutoNetworkedField]
    public StrangeMoodDefinition StrangeMood = new();

    [DataField, AutoNetworkedField]
    public SharedMood? SharedMood;

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
