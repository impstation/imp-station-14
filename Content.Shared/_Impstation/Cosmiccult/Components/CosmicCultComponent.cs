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
[AutoGenerateComponentState]
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
    ///     Amount of Entropy the cultist currently has.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float CurrentEntropy = 60f;

    /// <summary>
    ///     Maximum amount of Entropy a cultist can have.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxEntropy = 100f;

}


// CosmicCultComponent
