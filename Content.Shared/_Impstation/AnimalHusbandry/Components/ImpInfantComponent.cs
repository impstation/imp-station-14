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
    [DataField("offspringSettings")]
    public ProtoId<CloningSettingsPrototype> OffspringSettings = "BaseOffspringClone";

    /// <summary>
    /// Is this animal immediately born an NPC or do they start needing incubation
    /// </summary>
    [DataField("infantType"), ViewVariables(VVAccess.ReadWrite)]
    public InfantType InfantType = InfantType.Immediate;

    /// <summary>
    /// How long until the next growth stage
    /// </summary>
    [DataField("growthTime"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan GrowthTime = TimeSpan.FromSeconds(180);

    /// <summary>
    /// Next Growth stage of the animal
    /// </summary>
    [DataField("nextStage", required: true)]
    public EntProtoId NextStage;

    /// <summary>
    /// How long until we next grow up?
    /// </summary>
    public TimeSpan TimeUntilNextStage = TimeSpan.Zero;

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
