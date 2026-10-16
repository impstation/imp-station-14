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
