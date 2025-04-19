using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.WashingMachine.Components
{
    [Serializable, NetSerializable]
    public enum WashingMachineVisualState
    {
        Idle,
        Active,
        Broken,
        Bloody
    }
}
