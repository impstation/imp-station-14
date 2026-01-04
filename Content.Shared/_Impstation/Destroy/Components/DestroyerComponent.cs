using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Destroy;

/// <summary>
/// Allows an entity to eat whitelisted entities via an action.
/// Eaten mobs will be stored inside a container and released when the Destroyer is gibbed.
/// Eating something that fits their food preference will reward the Destroyer by being injected with a specific reagent.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(DestroySystem))]
public sealed partial class DestroyerComponent : Component
{
    /// <summary>
    /// Action prototype for Destroying.
    /// </summary>
    [DataField]
    public EntProtoId DestroyAction = "ActionDestroy";

    /// <summary>
    /// The spawned action entity for Destroying.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? DestroyActionEntity;

    /// <summary>
    /// The amount of time it takes to Destroy a mob.
    /// <remarks>
    [DataField, AutoNetworkedField]
    public float DestroyTime = 3f;

    /// <summary>
    /// The amount of time it takes to Destroy a structure.
    /// <remarks>
    /// NOTE: original intended design was to increase this proportionally with damage thresholds, but those proved quite difficult to get consistently. right now it Destroys the structure at a fixed timer.
    /// </remarks>
    /// </summary>
    [DataField, AutoNetworkedField]
    public float StructureDestroyTime = 10f;

    /// <summary>
    /// The sound to play when finishing Destroying something.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? SoundDestroy = new SoundPathSpecifier("/Audio/Effects/demon_consume.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    /// <summary>
    /// The sound to play when starting to Destroy a structure.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? SoundStructureDestroy = new SoundPathSpecifier("/Audio/Machines/airlock_creaking.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    /// <summary>
    /// Determines what things the Destroyer can consume.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist = new()
    {
        Components = new[]
        {
            "Door",
        }
    };
}

