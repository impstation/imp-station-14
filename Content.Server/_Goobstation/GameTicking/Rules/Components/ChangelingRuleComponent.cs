using Content.Server.GameTicking.Rules;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Roles;
using Content.Shared.Store;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Goobstation.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(GoobChangelingRuleSystem))]
public sealed partial class GoobChangelingRuleComponent : Component
{
    public readonly List<EntityUid> ChangelingMinds = new();

    public readonly List<ProtoId<StoreCategoryPrototype>> StoreCategories = new()
    {
        "ChangelingAbilityCombat",
        "ChangelingAbilitySting",
        "ChangelingAbilityUtility"
    };
}
