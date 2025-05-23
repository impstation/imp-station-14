using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.CombatModeVisuals;

public abstract class SharedCombatModeVisualsSystem : EntitySystem
{
    [Serializable, NetSerializable]
    public enum CombatModeVisualsVisuals : byte
    {
        Combat
    }
}
