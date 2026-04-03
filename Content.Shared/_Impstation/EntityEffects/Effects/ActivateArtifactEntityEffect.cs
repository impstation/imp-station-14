using Robust.Shared.Prototypes;
using Content.Shared.EntityEffects;

namespace Content.Shared._Impstation.EntityEffects.Effects;

// TODO IMP: I think this is obsolete..?
public sealed partial class ActivateArtifact : EntityEffectBase<ActivateArtifact>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("reagent-effect-guidebook-activate-artifact", ("chance", Probability));
}
