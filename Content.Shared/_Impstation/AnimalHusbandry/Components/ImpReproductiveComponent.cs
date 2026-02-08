using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Impstation.AnimalHusbandry.Components;
/// <summary>
/// Component that keeps track of all our variables for an animals reproductive abilities.
/// </summary>
[RegisterComponent]
[AutoGenerateComponentPause]
public sealed partial class ImpReproductiveComponent : Component
{
    /// <summary>
    /// Minimum amount of time a mob will wait before looking for a partner
    /// </summary>
    [DataField("minSearch"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan MinSearchAttemptInterval = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Maximum amount of time a mob will wait before looking for a partner
    /// </summary>
    [DataField("maxSearch"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan MaxSearchAttemptInterval = TimeSpan.FromSeconds(30);

    /// <summary>
    /// TODO: Make this use something else so this doesn't have to be manual
    /// Declares what type of mob this is. Such as a Cow or Pig
    /// </summary>
    [DataField("mobType")]
    public string MobType = "";

    /// <summary>
    /// List of Valid partners to breed with
    /// </summary>
    [DataField("validPartners")]
    public List<string> ValidPartners = new List<string>();

    /// <summary>
    /// Amount of hunger expended per birth
    /// </summary>
    [DataField("hungerPerBirth")]
    public int HungerPerBirth = 75;

    /// <summary>
    /// How long a mob will stay pregnant for
    /// </summary>
    [DataField("pregnancyLength"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan PregnancyLength = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Damage threshold a mob must hit in order to not be able to breed
    /// This is to prevent situations such as a mob close to crit looking to breed
    /// </summary>
    [DataField("maxBreedDamage"), ViewVariables(VVAccess.ReadOnly)]
    public int MaxBreedDamage = 50;

    /// <summary>
    /// Animals will not breed with the same Gender unless they are Agender
    /// </summary>
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

    public EntityUid PreviousPartner;

    public EntProtoId MobToBirth;
}

public enum AnimalGender
{
    Male,
    Female,
    Agender
}
