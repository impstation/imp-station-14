using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Zombies;

[RegisterComponent, NetworkedComponent]
public sealed partial class InitialInfectedComponent : Component
{
    [DataField]
    public ProtoId<FactionIconPrototype> StatusIcon = "InitialInfectedFaction";

    [DataField] //imp addition - special infected revivals
    public int RemainingRevives = 0;
}
