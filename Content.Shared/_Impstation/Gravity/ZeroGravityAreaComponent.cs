using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Gravity;
public abstract partial class SharedZeroGravityAreaComponent : Component
{
    /// <summary>
    /// Which physics fixture to use to detect entities.
    /// </summary>
    [DataField(readOnly: true), ViewVariables(VVAccess.ReadWrite)]
    public string Fixture = "antiGravity";

    /// <summary>
    /// Whether or not to put entities in the area into weightlessness.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled = true;

    /// <summary>
    /// <para>
    /// This is used to make overlapping ZeroGravityAreas work properly
    /// with client-side prediction with minimal awkward hiccups.
    /// </para>
    ///
    /// <para>
    /// Entites affected by the gravity areas will have <seealso cref="IsInZeroGravityAreaState.AreaFingerprint"/>
    /// that represents a bitmask of all the predict indices of the current gravity areas acting on them. The client
    /// will modify this when it predicts the player entering/leaving gravity areas, and the server will recalculate
    /// the fingerprint upon sending state.
    /// </para>
    ///
    /// <para>
    /// It's a little bit over-engineered, but it should guarantee that there are no prediction errors when there
    /// are less than 64 gravity areas.
    /// </para>
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public byte PredictIndex = 0;
}

[Serializable, NetSerializable]
public sealed partial class ZeroGravityAreaState : ComponentState
{
    public ZeroGravityAreaState(SharedZeroGravityAreaComponent comp)
    {
        Enabled = comp.Enabled;
        PredictIndex = comp.PredictIndex;
    }

    public bool Enabled;
    public byte PredictIndex;
}
