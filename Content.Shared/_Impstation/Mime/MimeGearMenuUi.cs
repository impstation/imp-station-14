using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Impstation.Mime;

[Serializable, NetSerializable]
public sealed class MimeGearMenuBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly Dictionary<int, MimeGearMenuSetInfo> Sets;
    public int MaxSelectedSets;

    public MimeGearMenuBoundUserInterfaceState(Dictionary<int, MimeGearMenuSetInfo> sets, int max)
    {
        Sets = sets;
        MaxSelectedSets = max;
    }
}

[Serializable, NetSerializable]
public sealed class MimeGearChangeSetMessage : BoundUserInterfaceMessage
{
    public readonly int SetNumber;

    public MimeGearChangeSetMessage(int setNumber)
    {
        SetNumber = setNumber;
    }
}

[Serializable, NetSerializable]
public sealed class MimeGearMenuApproveMessage : BoundUserInterfaceMessage
{
    public MimeGearMenuApproveMessage() { }
}

[Serializable, NetSerializable]
public enum MimeGearMenuUIKey : byte
{
    Key
};

[Serializable, NetSerializable, DataDefinition]
public partial struct MimeGearMenuSetInfo
{
    [DataField]
    public string Name;

    [DataField]
    public string Description;

    [DataField]
    public SpriteSpecifier Sprite;

    public bool Selected;

    public MimeGearMenuSetInfo(string name, string desc, SpriteSpecifier sprite, bool selected)
    {
        Name = name;
        Description = desc;
        Sprite = sprite;
        Selected = selected;
    }
}
