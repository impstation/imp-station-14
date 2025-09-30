using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Content.Shared.EntityEffects;

namespace Content.Shared.EntityEffects.Effects;

public sealed partial class ActivateArtifact : EventEntityEffect<ActivateArtifact>
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("reagent-effect-guidebook-activate-artifact", ("chance", Probability));
}
