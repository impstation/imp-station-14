using Content.Shared.Antag;
using Robust.Shared.GameStates;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;
using Content.Shared.Item;
using Content.Shared.Damage;
using System.Threading;

namespace Content.Shared._Impstation.Cosmiccult.Components;

/// <summary>
/// Added to mind role entities to tag that they are a cosmic cultist.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class CosmicCultComponent : Component
{
    #region Housekeeping

    /// <summary>
    /// The status icon prototype displayed for cosmic cultists.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "CosmicCultIcon";
    public CancellationTokenSource? DeconvertToken { get; set; }
    #endregion

    #region Ability Data
    public Dictionary<string, EntityUid?> Equipment = new();

    /// <summary>
    /// The duration of the doAfter for cosmic siphon
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan CosmicSiphonSpeed = TimeSpan.FromSeconds(4);

    /// <summary>
    /// The duration of the doAfter for cosmic blank
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan CosmicBlankSpeed = TimeSpan.FromSeconds(0.6f);

    /// <summary>
    /// The duration of cosmic blank's trip to the void
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan CosmicBlankDuration = TimeSpan.FromSeconds(22);

    /// <summary>
    /// The entity prototype to spawn in the cultist's hand after completing a cosmic siphon.
    /// </summary>
    [DataField]
    public EntProtoId<ItemComponent> CosmicSiphonResult = "MaterialCosmicCultEntropy1";

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
    #endregion

    #region VFX & SFX

    [DataField]
    public EntProtoId SpawnWisp = "MobCosmicWisp";

    [DataField]
    public EntProtoId LapseVFX = "CosmicLapseAbilityVFX";

    [DataField]
    public EntProtoId BlankVFX = "CosmicBlankAbilityVFX";

    [DataField]
    public SoundSpecifier LapseSFX = new SoundPathSpecifier("/Audio/_Impstation/CosmicCult/ability_lapse.ogg");

    [DataField]
    public SoundSpecifier BlankSFX = new SoundPathSpecifier("/Audio/_Impstation/CosmicCult/ability_blank.ogg");

    #endregion
}
