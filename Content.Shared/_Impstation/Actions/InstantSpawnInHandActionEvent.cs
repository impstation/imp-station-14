using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Actions;

/// <summary>
/// Spawn an entity in your hands.
/// </summary>
public sealed partial class InstantSpawnInHandActionEvent : InstantActionEvent
{
    /// <summary>
    /// What entity should be spawned.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Prototype;

    /// <summary>
    /// Sound collection to play when the item is summoned.
    /// </summary>
    [DataField]
    public SoundCollectionSpecifier? SummonSounds;
}
