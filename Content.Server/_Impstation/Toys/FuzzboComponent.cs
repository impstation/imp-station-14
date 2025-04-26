using Robust.Shared.Audio;
using Robust.Shared.Containers;

namespace Content.Server._Impstation.Toys;

/// <summary>
/// Hides radio channels on examine, allows for emag.
/// </summary>
[RegisterComponent]
public sealed partial class FuzzboComponent : Component

{
    /// <summary>
    /// Should the radio channels be examinable.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool Hidden;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("keyInsertionSound")]
    public SoundSpecifier KeyInsertionSound = new SoundPathSpecifier("eating");

    [ViewVariables]
    public Container KeyContainer = default!;
}

