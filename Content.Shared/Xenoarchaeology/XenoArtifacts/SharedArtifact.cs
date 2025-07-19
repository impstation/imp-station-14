using Robust.Shared.Serialization;
using Content.Shared.Actions; //#IMP

namespace Content.Shared.Xenoarchaeology.XenoArtifacts;

[Serializable, NetSerializable]
public enum SharedArtifactsVisuals : byte
{
    SpriteIndex,
    IsActivated,
    IsUnlocking
}

/// <summary>
///     Raised as an instant action event when a sentient artifact activates itself using an action. #IMP: Renamed to "XAEArtifact..." from "Artifact..."
/// </summary>
public sealed partial class ArtifactSelfActivateEvent : InstantActionEvent
{
}