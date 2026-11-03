using Content.Shared.Objectives;
using Robust.Shared.Serialization;
using Content.Shared._Starlight.CollectiveMind;
using Robust.Shared.Prototypes; // Starlight - Collective Minds

namespace Content.Shared.CharacterInfo;

[Serializable, NetSerializable]
public sealed class RequestCharacterInfoEvent : EntityEventArgs
{
    public readonly NetEntity NetEntity;

    public RequestCharacterInfoEvent(NetEntity netEntity)
    {
        NetEntity = netEntity;
    }
}

[Serializable, NetSerializable]
public sealed class CharacterInfoEvent : EntityEventArgs
{
    public readonly NetEntity NetEntity;
    public readonly string JobTitle;
    public readonly Dictionary<string, List<ObjectiveInfo>> Objectives;
    public readonly string? Briefing;
    public readonly Dictionary<ProtoId<CollectiveMindPrototype>, CollectiveMindMemberData>? CollectiveMinds; // Starlight - Collective Minds

    public CharacterInfoEvent(NetEntity netEntity,
        string jobTitle,
        Dictionary<string, List<ObjectiveInfo>> objectives,
        string? briefing,
        Dictionary<ProtoId<CollectiveMindPrototype>, CollectiveMindMemberData>? collectiveMinds) // Starlight - Collective Minds
    {
        NetEntity = netEntity;
        JobTitle = jobTitle;
        Objectives = objectives;
        Briefing = briefing;
        CollectiveMinds = collectiveMinds; // Starlight - Collective Minds
    }
}
