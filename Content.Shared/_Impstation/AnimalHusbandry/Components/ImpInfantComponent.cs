using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._Impstation.AnimalHusbandry.Components;

// It took everything for me to not call this Impfant component for the record
[RegisterComponent]
public sealed partial class ImpInfantComponent : Component
{
    // Notes on how this system is going to work
    // Planned idea is a system that utilises YAML to define stages of growth
    // Animals can then be set in the component on what steps they need to take
    // This approach allows for modularity with animal growths, allows for playing around with genes
    // (things like animals that can randomly skip stages and such)

    // Each stage would define what variables get changed within the component
    // as well as what new components need to be added & what components need removing if existing

    // Newborns would be split into 2 types: Immediate & Incubated

    // Immediate applies for Mammals (Cows, Pigs, etc.) allowing them to give birth to immediate offspring
    // Incubated applies for non-Mammals that instead may lay eggs or other things
    // Incubated start as a set entity with no growth timer that must be put into an incubator

    [DataField("infantType"), ViewVariables(VVAccess.ReadWrite)]
    public InfantType InfantType = InfantType.Immediate;

    // The parent this child should follow
    public EntityUid Parent;
}

public enum InfantType
{
    Immediate,
    Incubated
};
