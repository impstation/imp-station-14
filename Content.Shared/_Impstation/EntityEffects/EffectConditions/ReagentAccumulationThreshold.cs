using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.EffectConditions;

public sealed partial class ReagentAccumulationThreshold : EntityEffectCondition
{
    [DataField("limit", required:true)]
    public FixedPoint2 Limit { get; private set; }


    [DataField]
    private float _accumulated = 0.0f;

    [DataField]
    public ReagentId? Reagent;
    public override bool Condition(EntityEffectBaseArgs args)
    {
        if (args is EntityEffectReagentArgs reagentArgs)
        {
            if (Reagent == null && reagentArgs.Reagent?.ID != null)
            {
                var reagent = Reagent ?? new ReagentId(reagentArgs.Reagent!.ID, []);
            }

            if (Reagent == null)
                return true;
            _accumulated += reagentArgs.Scale.Float();

            if (_accumulated >= Limit)
            {
                _accumulated = 0;
                return true;
            }
        }

        return false;
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        ReagentPrototype? reagentProto = null;
        if (Reagent is not null)
            prototype.TryIndex(Reagent.Value.Prototype, out reagentProto);

        return Loc.GetString("reagent-effect-condition-guidebook-reagent-accumulation-threshold",
            ("reagent", reagentProto?.LocalizedName ?? Loc.GetString("reagent-effect-condition-guidebook-this-reagent")),
            ("limit", Limit ));
    }
}
