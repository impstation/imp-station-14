using Robust.Shared.Audio; // imp
using Robust.Shared.GameStates;

namespace Content.Shared.SprayPainter.Components;

/// <summary>
/// Items with this component can be used to recharge a spray painter.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SprayPainterAmmoSystem))]
public sealed partial class SprayPainterAmmoComponent : Component
{
    /// <summary>
    /// The value by which the charge in the spray painter will be recharged.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Charges = 15;

    /// <summary>
    /// IMP: The sound played when ammo is inserted into a spray painter.
    /// </summary>
    [DataField]
    public SoundSpecifier SoundInsert = new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/revolver_magin.ogg");
}
