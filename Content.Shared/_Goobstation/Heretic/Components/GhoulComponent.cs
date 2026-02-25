using Content.Shared.FixedPoint;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Heretic;

[RegisterComponent, NetworkedComponent]
/// <summary>
///     Component for Ghouls, dead bodies raised into servants by flesh heretics
/// </summary>
public sealed partial class GhoulComponent : Component
{
    /// <summary>
    ///     Total health for ghouls.
    /// </summary>
    [DataField] public FixedPoint2 TotalHealth = 50;

    [DataField]
    public ProtoId<FactionIconPrototype> StatusIcon = "GhouledFaction";
}
