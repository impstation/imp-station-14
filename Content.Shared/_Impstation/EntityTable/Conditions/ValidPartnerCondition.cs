using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.EntityTable;
using Content.Shared.EntityTable.Conditions;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.EntityTable.Conditions;
/// <summary>
/// Used by the Animal Husbandry System for checking if a mob has a valid parent to be born
/// For instance (Living in a world where we had moths) if two moths bred then a mothroach could not be born
/// If a moth and a cockroach bred then this would allow a mothroach to be born
/// </summary>
public sealed partial class ValidPartnerCondition : EntityTableCondition
{
    public const string PartnerContextKey = "Partner";

    [DataField("validPartners")]
    public List<string> ValidPartners = new List<string>();

    /// <summary>
    ///  Checks if our given mob matches any of the required mobs for a single offspring
    /// </summary>
    /// <param name="root"></param>
    /// <param name="entMan"></param>
    /// <param name="proto"></param>
    /// <param name="ctx"></param>
    /// <returns></returns>
    protected override bool EvaluateImplementation(EntityTableSelector root, IEntityManager entMan, IPrototypeManager proto, EntityTableContext ctx)
    {
        if (!ctx.TryGetData<string>(PartnerContextKey, out var partner))
            return false;

        // If there are no names provided then all partners are valid
        if (ValidPartners.Count == 0)
            return true;

        // Check if the one we are breeding with is the mob required for this offspring
        foreach (var partnerName in ValidPartners)
        {
            if (partnerName == partner)
                return true;
        }

        return false;
    }
}
