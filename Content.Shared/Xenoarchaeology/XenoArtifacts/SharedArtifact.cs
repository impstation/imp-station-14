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
