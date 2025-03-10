using Robust.Shared.GameStates;

namespace Content.Shared.Anomaly.Effects.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedWallAnomalySystem))]

public sealed partial class WallSpawnAnomalyComponent : Component
{

}