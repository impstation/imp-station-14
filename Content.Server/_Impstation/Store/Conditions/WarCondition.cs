using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.Store;

namespace Content.Server._Impstation.Store.Conditions;

/// <summary>
/// Filters out a listing based on war declaration
/// </summary>
public sealed partial class WarCondition : ListingCondition
{
    /// <summary>
    /// If its buyable after war is declared or only before war is declared
    /// </summary>
    [DataField]
    public bool AfterWar = true;

    public override bool Condition(ListingConditionArgs args)
    {
        var gameTicker = args.EntityManager.System<GameTicker>();

        var warDeclared = false;
        foreach (var rule in gameTicker.GetActiveGameRules<NukeopsRuleComponent>())
        {
            if (rule.Comp.WarDeclaredTime == null)
                continue;

            warDeclared = true;
            break;
        }

        return warDeclared == AfterWar;
    }
}
