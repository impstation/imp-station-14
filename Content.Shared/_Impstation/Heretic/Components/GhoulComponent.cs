using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Heretic;

[RegisterComponent, NetworkedComponent]
/// <summary>
/// Component for Ghouls, dead bodies raised into servants by flesh heretics
/// </summary>
public sealed partial class GhoulComponent : Component
{
    /// <summary>
    ///     What a ghoul's health is divided by.
    /// </summary>
    [DataField] public FixedPoint2 HealthDivisor = 4;

    /// <summary>
    ///     Maximum health used for ghoul health calculations.
    /// </summary>
    [DataField] public FixedPoint2 MaxHealth = 200;

    /// <summary>
    ///     Fallback for if dead threshold is null.
    /// </summary>
    [DataField] public FixedPoint2 FallbackHealth = 50;
    /// <summary>
    ///     Ghoul HUD icon.
    /// </summary>
    [DataField] public ProtoId<FactionIconPrototype> StatusIcon = "GhouledFaction";
}
