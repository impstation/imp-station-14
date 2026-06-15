using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._Impstation.BountyHunter;


/// <summary>
/// Component used as a marker for an item marked by the WeaponRecall ability. I could've just added access to the wizard version of this,
/// but didn't want to edit an upstream file for it
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedWeaponRecallSystem))]
public sealed partial class WeaponRecallMarkerComponent : Component
{
    /// <summary>
    /// The action that marked this item.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? MarkedByAction;
}
