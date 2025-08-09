using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.EntityEffects.Effects;

public sealed partial class MakeTame : EventEntityEffect<MakeTame>
{
    //this is basically completely copied from MakeSentient, but with a bit of changes to how the ghost roles are listed
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-make-sentient", ("chance", Probability));

}
