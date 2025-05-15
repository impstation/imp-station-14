using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Impstation.Captain;

[Serializable, NetSerializable]
public sealed class CaptainSwordMenuBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly Dictionary<int, CaptainSwordMenuSetInfo> Sets;
    public int MaxSelectedSets;

    public CaptainSwordMenuBoundUserInterfaceState(Dictionary<int, CaptainSwordMenuSetInfo> sets, int max)
    {
        Sets = sets;
        MaxSelectedSets = max;
    }
}

[Serializable, NetSerializable]
public sealed class CaptainSwordChangeSetMessage : BoundUserInterfaceMessage
{
    public readonly int SetNumber;

    public CaptainSwordChangeSetMessage(int setNumber)
    {
        SetNumber = setNumber;
    }
}

[Serializable, NetSerializable]
public sealed class CaptainSwordMenuApproveMessage : BoundUserInterfaceMessage
{
    public CaptainSwordMenuApproveMessage() { }
}

[Serializable, NetSerializable]
public enum CaptainSwordMenuUIKey : byte
{
    Key
};

[Serializable, NetSerializable, DataDefinition]
public partial struct CaptainSwordMenuSetInfo
{
    [DataField]
    public string Name;

    [DataField]
    public string Description;

    [DataField]
    public SpriteSpecifier Sprite;

    public bool Selected;

    public CaptainSwordMenuSetInfo(string name, string desc, SpriteSpecifier sprite, bool selected)
    {
        Name = name;
        Description = desc;
        Sprite = sprite;
        Selected = selected;
    }
}
