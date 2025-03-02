using Robust.Shared.Serialization;

namespace Content.Shared._Wizden.Geras;

[Access(typeof(SharedGerasSystem))]
public abstract partial class SharedGerasComponent : Component
{
}

[Serializable, NetSerializable]
public enum GeraColor
{
    Color,
}
