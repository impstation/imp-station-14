using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Server._Impstation.AnimalHusbandry.BreedEffects;
using Content.Shared.EntityTable;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Impstation.AnimalHusbandry.Components;
/// <summary>
/// Component that keeps track of all our variables for an animals reproductive abilities.
/// </summary>
[RegisterComponent]
[AutoGenerateComponentPause]
public sealed partial class ImpReproductiveComponent : Component
{
    [DataField("minSearchAttemptInterval"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan MinSearchAttemptInterval = TimeSpan.FromSeconds(10);

    [DataField("maxSearchAttemptInterval"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan MaxSearchAttemptInterval = TimeSpan.FromSeconds(30);

    [DataField("reproductiveGroup")]
    public string ReproductiveGroup = "MobNone";

    // What type of mob is this
    [DataField("mobType")]
    public string MobType = "";

    // List of Partners this mob can breed with
    [DataField("validPartners")]
    public List<string> ValidPartners = new List<string>();

    [DataField("hungerPerBirth")]
    public int HungerPerBirth = 75;

    [DataField("pregnancyLength"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan PregnancyLength = TimeSpan.FromSeconds(60);

    // Maximum amount of damage allowed before the mob gives up trying to breed
    [DataField("maxBreedDamage"), ViewVariables(VVAccess.ReadOnly)]
    public int MaxBreedDamage = 50;

    // Animals of the same Gender won't breed EXCEPT for if they are Agender, which can breed with any
    [DataField("gender"), ViewVariables(VVAccess.ReadWrite)]
    public AnimalGender Gender = AnimalGender.Agender;

    /// <summary>
    /// Format the chosen animals like this within your YAML.
    /// This variable allows animals to have multiple offspring
    ///     possibleInfants: !type:GroupSelector
    ///children:
    ///  - id: Example
    ///    weight: 10
    /// </summary>
    [DataField("possibleInfants"), ViewVariables(VVAccess.ReadWrite)]
    public EntityTableSelector? PossibleInfants = default!;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool Pregnant = false;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan EndPregnancy = TimeSpan.Zero;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextSearch = TimeSpan.Zero;

    [DataField("breedEffects")]
    public List<BaseBreedEffect> BreedEffects = new List<BaseBreedEffect>();

    public EntityUid PreviousPartner;

    public EntProtoId MobToBirth;
}

public enum AnimalGender
{
    Male,
    Female,
    Agender
}
