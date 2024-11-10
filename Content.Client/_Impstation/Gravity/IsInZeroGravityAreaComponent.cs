using Robust.Shared.GameStates;

namespace Content.Client.Gravity;

[RegisterComponent, NetworkedComponent]
public sealed partial class IsInZeroGravityAreaComponent : Component
{
    /// <summary>
    /// When this number is non-zero, the entity is most likely weightless.
    /// </summary>
    ///
    /// This is a bitmask of all the entity IDs of the affecting areas bitwise-OR'd together.
    /// Prediction calculation of movement needs to be blistering fast to remain seamless, so I
    /// am utilizing some bitwise tricks to try and estimate whether or not the player is being
    /// affected by a ZeroGravityArea.
    ///
    /// When a player enters a ZeroGravityArea, the area's NetEntity ID will be bitwise-OR'd into
    /// the fingerprint.
    /// When a player leaves a ZeroGravityArea, the fingerprint will be bitwise-AND'ed with the
    /// negation of the area's NetEntity ID.
    /// This leaves us with an extremely rough approximate idea of whether or not we are still
    /// being affected by an area. Theoretically, it should guarantee correctness in the case
    /// of two overlapping ZeroGravityAreas, and decently high chance of correctness with
    /// three overlapping areas.
    /// If there's more, fuck it. I tried.
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public int AreaFingerprint = 0;
}
