using Content.Shared.Antag;
using Robust.Shared.GameStates;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;
using Content.Shared.Item;
using Content.Shared.Damage;

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
        "ActionSiphonEntropy",
        "ActionCosmicToolToggle"
    };

    #endregion
    public Dictionary<string, EntityUid?> Equipment = new();

    #region Abilities

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

    #region Cosmic Siphon

    /// <summary>
    /// The duration of the doAfter for performing a cosmic siphon
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan CosmicSiphonDuration = TimeSpan.FromSeconds(3);

    /// <summary>
    ///     The entity prototype to spawn in the cultist's hand after
    ///     completing a cosmic siphon
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId<ItemComponent> CosmicSiphonResult = "SpaceCash100"; // TODO: REPLACE

    /// <summary>
    /// The damage to apply upon a successful cosmic siphon
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier CosmicSiphonDamage = new()
    {
        DamageDict = new() {
            { "Asphyxiation", 25 }
        }
    };

    #endregion // Cosmic Siphon

    #endregion // Abilities

}


// CosmicCultComponent
