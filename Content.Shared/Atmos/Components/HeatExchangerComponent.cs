using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Components;

/// <summary>
/// Handles transfer data from server calculations to client visuals.
/// All logic is serverside.
/// </summary>
// [Access(typeof(SharedHeatExchangerSystem))]
public abstract partial class SharedHeatExchangerComponent : Component
{
}

[Serializable, NetSerializable]
public enum HeatExchangerVisuals
{
    On,
    Color
}

public enum HeatExchangerVisualLayers
{
    Base,
    Inert,
    Glowing,
}
