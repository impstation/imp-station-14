using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Heretic.Ritual;

[RegisterComponent]
public sealed partial class TransmuteBehaviorComponent : Component
{
    /// <summary>
    /// What entities will be spawned on success.
    /// </summary>
    [DataField] public Dictionary<EntProtoId, int> OutputItems = [];
}
