using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.TileFires;

/// <summary>
///     Handles growth behavior for tile fires, as well as things like requiring oxygen.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ESTileFireComponent : Component
{
    /// <summary>
    ///     Prototype to spawn when spreading.
    /// </summary>
    [DataField]
    public EntProtoId Prototype = "ESTileFire";

    [DataField]
    public float MinFirestacksToSpread = 10;

    [DataField]
    public float FirestacksRemoveOnSpread = 3;

    [DataField]
    public float BaseSpreadChance = 0.66f;

    [DataField]
    public float MinimumOxyMolesToSpread = 0.5f;
}
