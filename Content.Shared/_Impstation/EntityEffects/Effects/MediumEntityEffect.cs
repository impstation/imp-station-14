using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.EntityEffects.Effects;

public sealed partial class Medium : EntityEffectBase<Medium>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Grants whoever drinks this the ability to see ghosts for a while";
    }
}
