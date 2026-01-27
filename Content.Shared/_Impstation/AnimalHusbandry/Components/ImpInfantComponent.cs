using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Cloning;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.AnimalHusbandry.Components;

// It took everything for me to not call this Impfant component for the record
[RegisterComponent]
public sealed partial class ImpInfantComponent : Component
{
    // The information that carries over between growth stages
    [DataField("offspringSettings")]
    public ProtoId<CloningSettingsPrototype> OffspringSettings = "BaseOffspringClone";

    // Is this animal immediately born an NPC or do they start needing incubation
    [DataField("infantType"), ViewVariables(VVAccess.ReadWrite)]
    public InfantType InfantType = InfantType.Immediate;

    // How long until the next growth stage
    [DataField("growthTime"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan GrowthTime = TimeSpan.FromSeconds(180);

    // Next Growth stage of the animal
    [DataField("nextStage", required: true)]
    public EntProtoId NextStage;

    // How long until we next grow up?
    public TimeSpan TimeUntilNextStage = TimeSpan.Zero;

    // The parent this child should follow
    public EntityUid Parent;
}

public enum InfantType
{
    Immediate,
    Incubated
};
