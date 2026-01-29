using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.AnimalHusbandry.Components;

/// <summary>
/// Component used by the IncubationSystem to track what comes out, how long it takes to do so and other things
/// </summary>
[RegisterComponent]
public sealed partial class IncubationComponent : Component
{
    // How long this egg incubates for
    [DataField("incubationTime")]
    public TimeSpan IncubationTime = TimeSpan.FromSeconds(90);

    // What comes out when the incubation is done?
    [DataField("incubatedResult"), ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId IncubatedResult;

}
