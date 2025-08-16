using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Tourist.Components;

/// <summary>
/// Component placed on a mob to make it a tourist and track photographed entities
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedTouristCameraSystem))]
public sealed partial class PhotoObjectiveComponent : Component
{
    [DataField("targets"), AutoNetworkedField]
    public HashSet<string> ValidTargets = new();

    [DataField("photosTaken"), ViewVariables(VVAccess.ReadWrite)]
    public int CurrentCount;
}
