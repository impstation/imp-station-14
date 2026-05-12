using Content.Shared.Cloning;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.AnimalHusbandry.Components;

/// <summary>
/// The Component that holds the info for infants. This also allows mobs to by tracked by the AnimalHusbandrySystem for growth
/// It took everything for me to not call this Impfant component for the record
/// </summary>
[RegisterComponent]
public sealed partial class ImpInfantComponent : Component
{
    /// <summary>
    /// The information that carries over between growth stages
    /// </summary>
    [DataField]
    public ProtoId<CloningSettingsPrototype> OffspringSettings = "BaseOffspringClone";

    /// <summary>
    /// Is this animal immediately born an NPC or do they start needing incubation
    /// </summary>
    [DataField]
    public InfantType InfantType = InfantType.Immediate;

    /// <summary>
    /// How long until the next growth stage
    /// </summary>
    [DataField]
    public TimeSpan GrowthTime = TimeSpan.FromSeconds(180);

    /// <summary>
    /// Next Growth stage of the animal
    /// </summary>
    [DataField(required: true)]
    public EntProtoId NextStage;

    /// <summary>
    /// How long until we next grow up?
    /// </summary>
    public TimeSpan CurrentGrowthTime = TimeSpan.Zero;

    /// <summary>
    /// The parent this child should follow
    /// </summary>
    public EntityUid Parent;
}

public enum InfantType
{
    Immediate,
    Incubated
};

/// <summary>
///     Whether or not this entity is currently capable of growing up.
/// </summary>
/// <param name="Cancelled">True if this entity cannot grow currently.</param>
[ByRefEvent]
public record struct InfantCanGrowEvent(bool Cancelled = false);
