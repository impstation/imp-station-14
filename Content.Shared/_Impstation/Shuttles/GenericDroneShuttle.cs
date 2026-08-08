using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Impstation.Shuttles;

/// <summary>
/// Tag component for generic drone-control shuttles. Dont use more than one per map.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GenericDroneShuttleComponent : Component
{
    /*
     * Still needed for drone console for now.
     */
}
