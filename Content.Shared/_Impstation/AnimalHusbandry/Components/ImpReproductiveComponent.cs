using Content.Shared._Impstation.AnimalHusbandry.Prototypes;
using Content.Shared.Nutrition.Components;
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
    [DataField("minSearch")]
    public TimeSpan MinSearchAttemptInterval = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Maximum amount of time a mob will wait before looking for a partner
    /// </summary>
    [DataField("maxSearch")]
    public TimeSpan MaxSearchAttemptInterval = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Amount of hunger expended per birth
    /// </summary>
    [DataField]
    public int HungerPerBirth = 75;

    /// <summary>
    /// How long a mob will stay pregnant for
    /// </summary>
    [DataField]
    public TimeSpan PregnancyLength = TimeSpan.FromSeconds(120);

    /// <summary>
    /// Damage threshold a mob must hit in order to not be able to breed
    /// This is to prevent situations such as a mob close to crit looking to breed
    /// </summary>
    [DataField]
    public int MaxBreedDamage = 50;

    /// <summary>
    ///     Minimum hunger threshold needed to breed.
    /// </summary>
    [DataField]
    public HungerThreshold MinimumHungerThreshold = HungerThreshold.Okay;

    /// <summary>
    ///     Minimum thirst threshold needed to breed.
    /// </summary>
    [DataField]
    public ThirstThreshold MinimumThirstThreshold = ThirstThreshold.Okay;

    /// <summary>
    /// Animals will not breed with the same Gender unless they are Agender
    /// </summary>
    [DataField("sex")]
    public AnimalGender Gender = AnimalGender.None;

    /// <summary>
    /// The settings used for things such as an animals compatible partners and
    /// the possible offspring that it can have.
    /// </summary>
    [DataField("breedPrototype")]
    public ProtoId<BreedSettingsPrototype>? BreedSettings;

    /// <summary>
    ///     The next time we should attempt to search for a partner.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextSearch = TimeSpan.Zero;

    /// <summary>
    ///     The last entity that this entity has reproduced with successfully.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid PreviousPartner;
}

public enum AnimalGender
{
    Male,
    Female,
    None
}
