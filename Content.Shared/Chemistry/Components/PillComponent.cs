using Robust.Shared.GameStates;

namespace Content.Shared.Chemistry.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class PillComponent : Component
{
    /// <summary>
    /// The pill id. Used for networking & serializing pill visuals.
    /// </summary>
    [AutoNetworkedField]
    [DataField("pillType")]
    [ViewVariables(VVAccess.ReadWrite)]
    public uint PillType;

    /// <summary>
    /// If the pill should be of a random type. Imp addition
    /// </summary>
    [AutoNetworkedField]
    [DataField("randomType")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool RandomType = false;
}
