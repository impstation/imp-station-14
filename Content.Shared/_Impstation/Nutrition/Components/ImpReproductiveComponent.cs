using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.EntityTable;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Nutrition.Components;
/// <summary>
/// Component that keeps track of all our variables for an animals reproductive abilities.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ImpReproductiveComponent : Component
{
    [DataField("minSearchAttemptInterval"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan MinSearchAttemptInterval = TimeSpan.FromSeconds(10);

    [DataField("maxSearchAttemptInterval"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan MaxSearchAttemptInterval = TimeSpan.FromSeconds(30);

    [DataField("partnerWhiteList", required: true)]
    public EntityWhitelist PartnerWhiteList = default!;

    [DataField("reproductiveGroup", required: true)]
    public string ReproductiveGroup = "MobNone";

    [DataField("hungerPerBirth", required: true)]
    public int HungerPerBirth = 75;

    [DataField("pregnancyLength", required: true), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan PregnancyLength = TimeSpan.FromSeconds(60);

    // Maximum amount of damage allowed before the mob gives up trying to breed
    [DataField("maxBreedDamage"), ViewVariables(VVAccess.ReadOnly)]
    public int MaxBreedDamage = 50;

    // Animals of the same Gender won't breed EXCEPT for if they are Agender, which can breed with any
    [DataField("gender", required: true), ViewVariables(VVAccess.ReadWrite)]
    public AnimalGender Gender = AnimalGender.Agender;

    /// <summary>
    /// Format the chosen animals like this within your YAML.
    /// This variable allows animals to have multiple offspring
    ///     possibleInfants: !type:GroupSelector
    ///children:
    ///  - id: Example
    ///    weight: 10
    /// </summary>
    [DataField("possibleInfants", required: true), ViewVariables(VVAccess.ReadWrite)]
    public EntityTableSelector PossibleInfants = default!;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool Pregnant = false;

    public TimeSpan EndPregnancy = TimeSpan.Zero;
    public TimeSpan NextSearch = TimeSpan.Zero;
}

public enum AnimalGender
{
    Male,
    Female,
    Agender
}
