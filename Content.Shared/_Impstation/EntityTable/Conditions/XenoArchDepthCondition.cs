using Content.Shared.EntityTable;
using Content.Shared.EntityTable.Conditions;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.GameTicking.Rules;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.EntityTable.Conditions;

public sealed partial class XenoArchDepthCondition : EntityTableCondition
{
    [DataField]
    public List<int> Depths = new();

    protected override bool EvaluateImplementation(EntityTableSelector root,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx)
    {
        if (!ctx.TryGetData<int>("Depth", out var depth))
            return false;

        if (depth > 5)
            depth = 5;

        return Depths.Contains(depth);
    }
}
