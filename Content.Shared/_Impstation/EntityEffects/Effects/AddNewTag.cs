using Content.Shared.EntityEffects;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Impstation.EntityEffects.Effects;

public sealed partial class AddNewTag : EntityEffect

{
    [DataField]
    public ProtoId<TagPrototype> AddTag;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    => Loc.GetString("reagent-effect-guidebook-tagadd", ("chance", Probability));
    public override void Effect(EntityEffectBaseArgs args)
    {

        //get these out of the args to be less annoying
        var entMan = args.EntityManager;
        var ent = args.TargetEntity;

        //get the tag from the ent manager
        var tagSystem = entMan.System<TagSystem>();

        //add the tag
        tagSystem.AddTag(ent, AddTag);
    }

}