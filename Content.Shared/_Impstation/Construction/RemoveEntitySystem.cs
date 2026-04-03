using Content.Shared.Tag;
using Robust.Shared.Containers;

namespace Content.Shared._Impstation.Construction;

public sealed class RemoveEntitySysten : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    public bool IsValid(EntityUid uid, BaseContainer container, string tag)
    {
        if (string.IsNullOrEmpty(tag) || !_tag.HasTag(uid, tag))
            return false;

        if (_container.ContainsEntity(container.Owner, uid))
            return false;

        return true;

    }
}
