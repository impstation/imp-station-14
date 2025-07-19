using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.WashingMachine
{
    [Serializable, NetSerializable]
    public enum WashingMachineVisualLayers
    {
        Base,
        Fabric,
        Reagent,
        Door,
        Cycle
    }

    [Serializable, NetSerializable]
    public enum WashingMachineVisualState
    {
        Idle,
        Active,
        Broken,
        Bloody
    }
}
