using Content.Shared.Antag;
using Robust.Shared.GameStates;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;

namespace Content.Shared._Impstation.Cosmiccult.Components;

/// <summary>
/// Added to mind role entities to tag that they are a cosmic cultist.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedCosmicCultSystem))]
public sealed partial class CosmicCultComponent : Component
{
    /// <summary>
    /// The status icon prototype displayed for cosmic cultists.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "CosmicCultIcon";

    public override bool SessionSpecific => true;
}

// CosmicCultComponent
