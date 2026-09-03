using Robust.Shared.Serialization;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Ledges;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LedgeComponent : Component
{
    /// <summary>
    /// Maintained hashset of entities currently passing over the ledge
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> CollideExceptions = new();

    /// <summary>
    /// A whitelist of the things that will interact with the ledge.
    /// Things not in this whitelist will be ignored by default.
    /// </summary>
    [DataField]
    public EntityWhitelist? InteractWhitelist;
    
    /// <summary>
    /// Angle in radians/pi of the lower bound of the approach angles that should be blocked.
    /// </summary>
    [DataField]
    public float BlockBoundLower = -0.25f;

    /// <summary>
    /// Angle in radians/pi of the upper bound of the approach angles that should be blocked.
    /// </summary>
    [DataField]
    public float BlockBoundUpper = 0.25f;
}
