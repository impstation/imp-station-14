using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Tourist.Components;

/// <summary>
/// Component placed on a mob to make it a tourist and track photographed entities
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedTouristCameraSystem))]
public sealed partial class TouristComponent : Component
{
    [DataField]
    public HashSet<EntityUid> PhotographedEntities = new();
}
