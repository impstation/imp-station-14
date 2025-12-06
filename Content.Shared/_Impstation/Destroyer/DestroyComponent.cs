using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Destroyer;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DestroyComponent : Component
{
    /// <summary>
    /// Action prototype for destroying structures.
    /// </summary>
    [DataField]
    public EntProtoId DestroyAction = "ActionDestroy";

    /// <summary>
    /// The spawned action entity for destroying.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? DestroyActionEntity;

    /// <summary>
    /// The amount of time it takes to destroy a structure.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DestroyTime = 3f;

    /// <summary>
    /// The sound to play when finishing destruction.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? SoundDestroy = new SoundPathSpecifier("/Audio/Effects/demon_consume.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    /// <summary>
    /// The whitelist of allowed targets for destruction (e.g., walls, doors, windows).
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist = new()
    {
        Components = new[]
        {
            "Wall",
            "Door",
            "Window",
        }
    };
}
