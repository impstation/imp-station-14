using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Gravity;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ZeroGravityAreaComponent : Component
{
    /// <summary>
    /// Which physics fixture to use to detect entities.
    /// </summary>
    [DataField(readOnly: true), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public string Fixture = "antiGravity";

    /// <summary>
    /// Whether or not to put entities in the area into weightlessness.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public bool Enabled = true;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    [AutoNetworkedField]
    public HashSet<NetEntity> AffectedEntities = new();

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
    [AutoNetworkedField]
    public byte PredictIndex = 0;
}
