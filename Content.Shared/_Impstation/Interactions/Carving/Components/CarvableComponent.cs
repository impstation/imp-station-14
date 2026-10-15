using Content.Shared.Tools;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Impstation.Interactions.Carving.Components;

[RegisterComponent]
public sealed partial class CarvableComponent : Component
{
    [DataField("tool"), ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<ToolQualityPrototype> Tool = "Cutting";

    [DataField("carvedSprites"), ViewVariables(VVAccess.ReadWrite)]
    public HashSet<SpriteSpecifier> Carves = new HashSet<SpriteSpecifier>();

    public bool Carved;
}

[Serializable, NetSerializable]
public enum CarvedVisuals
{
    Carved,
}

public enum CarveType
{
    Full,   // Full Sprite Change
    Partial // Overlays a new Sprite on top of the base
}

[Serializable, NetSerializable]
public enum CarvingUiKey
{
    Key
}

public partial struct CarvedEvent
{
    public EntityUid Carved;
    public SpriteSpecifier Sprite;
    public CarveType CarveType;

    public CarvedEvent(
        EntityUid carved,
        SpriteSpecifier sprite,
        CarveType carveType
        )
    {
        Carved = carved;
        Sprite = sprite;
        CarveType = carveType;
    }
}
