using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Impstation.Interactions.Carving.DoAfters;

[Serializable, NetSerializable]
public sealed partial class PersonaliseDoAfterEvent : SimpleDoAfterEvent
{
    public NetEntity Entity;
    public SpriteSpecifier Sprite;
    public string CarveName;

    public PersonaliseDoAfterEvent
        (
        NetEntity entity,
        SpriteSpecifier sprite,
        string carveName
        )
    {
        Entity = entity;
        Sprite = sprite;
        CarveName = carveName;
    }
}

