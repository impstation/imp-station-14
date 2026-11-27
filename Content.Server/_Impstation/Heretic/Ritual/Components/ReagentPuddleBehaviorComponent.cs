using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Heretic.Ritual;

[RegisterComponent]
public sealed partial class ReagentPuddleBehaviorComponent : Component
{
    /// <summary>
    /// Whitelist for reagents that can be used for the given ritual.
    /// </summary>
    [DataField] public List<ProtoId<ReagentPrototype>>? Reagents { get; set; }
}
