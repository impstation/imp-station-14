using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Impstation.Interactions.Carving.Events;

[Serializable, NetSerializable]
public readonly struct CarvingPatternEntry(SpriteSpecifier sprite)
{
    public readonly SpriteSpecifier Sprite = sprite;
}

[Serializable, NetSerializable]
public sealed class CustomisePersonalisationEvent : EntityEventArgs
{
    public NetEntity Entity;
    public SpriteSpecifier Sprite;
    public Enum Mapping;

    public CustomisePersonalisationEvent(NetEntity entity, SpriteSpecifier sprite, Enum mapping)
    {
        Entity = entity;
        Sprite = sprite;
        Mapping = mapping;
    }
}

[Serializable, NetSerializable]
public sealed class AddPersonalisationState : EntityEventArgs
{
    public NetEntity Entity;
    public string StateExtension;
    public Enum Mapping;

    public bool AddVisible;

    public AddPersonalisationState(NetEntity entity, string stateExtension, Enum mapping, bool addVisible = false)
    {
        Entity = entity;
        StateExtension = stateExtension;
        Mapping = mapping;
        AddVisible = addVisible;
    }
}
