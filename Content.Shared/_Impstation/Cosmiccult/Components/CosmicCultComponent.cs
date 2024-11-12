using Content.Shared.Antag;
using Robust.Shared.GameStates;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;

namespace Content.Shared._Impstation.Cosmiccult.Components;

/// <summary>
/// Added to mind role entities to tag that they are a cosmic cultist.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CosmicCultComponent : Component
{
    #region Stuff

    /// <summary>
    /// The status icon prototype displayed for cosmic cultists.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "CosmicCultIcon";

    public override bool SessionSpecific => true;
    public readonly List<ProtoId<EntityPrototype>> BaseCosmicCultActions = new()
    {
        // "ActionSiphonEntropy",
        "ActionToggleCosmicTool"
    };

    #endregion
    public Dictionary<string, EntityUid?> Equipment = new();

    /// <summary>
    ///     Amount of entropy the cultist currently has.
    /// </summary>
    public float Entropy = 60f;
}


// CosmicCultComponent
