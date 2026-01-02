using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.EntityEffects.Effects;

//this is basically completely copied from MakeSentient, but with a bit of changes to how the ghost roles are listed
/// <inheritdoc cref="EntityEffect"/>
public sealed partial class MakeTame : EntityEffectBase<MakeTame>
{
    /// <summary>
    /// Description for the ghost role created by this effect.
    /// </summary>
    [DataField]
    public LocId RoleDescription = "ghost-role-information-nonantagonist-freeagent-tame";

    /// <summary>
    /// Rules for the ghost role created by this effect.
    /// </summary>
    [DataField]
    public LocId RoleRules = "ghost-role-information-tame-rules";

    /// <summary>
    /// Whether we give the target the ability to speak coherently.
    /// </summary>
    [DataField]
    public bool AllowSpeech = true;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-make-sentient", ("chance", Probability));

}

