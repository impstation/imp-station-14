using Content.Shared._Impstation.TraitorFlavor;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Store;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server._Impstation.Store.Conditions;

/// <summary>
/// Allows a store listing to be filtered based on the user's traitor employer.
/// </summary>
public sealed partial class BuyerEmployerCondition : ListingCondition
{
    /// <summary>
    /// A whitelist of traitor employers that can purchase this listing. Only one needs to be found.
    /// </summary>
    [DataField("whitelist", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<TraitorEmployerPrototype>))]
    public HashSet<string>? Whitelist;

    /// <summary>
    /// A blacklist of traitor employers that cannot purchase this listing. Only one needs to be found.
    /// </summary>
    [DataField("blacklist", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<TraitorEmployerPrototype>))]
    public HashSet<string>? Blacklist;

    public override bool Condition(ListingConditionArgs args)
    {
        var ent = args.EntityManager;
        var roleSystem = ent.System<SharedRoleSystem>();

        if (!ent.HasComponent<MindComponent>(args.Buyer))
            return true; // inanimate objects don't have minds

        roleSystem.MindHasRole<TraitorRoleComponent>(args.Buyer, out var traitorRole);

        /*
        if (traitorRole?.Comp2.Employer == null)
            return true;

        if (Blacklist != null)
        {
            if (Blacklist.Contains(traitorRole.Value.Comp2.Employer.ID))
            {
                return false;
            }
        }

        if (Whitelist != null)
        {
            if (!Whitelist.Contains(traitorRole.Value.Comp2.Employer.ID))
            {
                return false;
            }
        }
        */

        return true;
    }
}
