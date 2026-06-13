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
    /// If the sprite should be decided by the type. Imp addition
    /// </summary>
    [AutoNetworkedField]
    [DataField("spriteUsesType")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool SpriteUsesType = true;
}
