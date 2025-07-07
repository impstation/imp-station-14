using Content.Shared.Humanoid;
using Content.Shared.StatusIcon;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Arcfiend;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ArcfiendComponent : Component
{
    #region Prototypes

    public readonly List<EntProtoId> BaseArcfiendActions = new()
    {
        "ActionSapPower",
        "ActionDischarge",
        "ActionFlash",
        "ActionArcFlash",
        "ActionRideTheLightning",
        "ActionJammingField",
        "ActionJolt"
    };

    #endregion

    /// <summary>
    ///    Amount of energy the arcfiend has.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Energy = 0f;

    /// <summary>
    ///     Maximum amount of energy the arcfiend can have.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxEnergy = 2500f;
}
