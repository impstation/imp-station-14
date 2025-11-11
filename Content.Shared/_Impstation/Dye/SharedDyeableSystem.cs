using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Dye

{
    public abstract class SharedDyeableSystem : EntitySystem { }

    [Serializable, NetSerializable]
    public sealed class DyedComponentState : ComponentState
    {
        public Color CurrentColor = default!;
    }
}
