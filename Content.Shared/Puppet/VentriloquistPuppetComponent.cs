using Robust.Shared.GameStates;

namespace Content.Shared.Puppet;

[RegisterComponent, NetworkedComponent]
public sealed partial class VentriloquistPuppetComponent : Component
{
    // Frontier edit below
    [DataField]
    public List<LocId> RemoveHand = [];

    [DataField]
    public List<LocId> RemovedHand = [];

    [DataField]
    public List<LocId> InsertHand = [];

    [DataField]
    public List<LocId> InsertedHand = [];

    [DataField]
    public List<LocId> PuppetRoleName = [];

    [DataField]
    public List<LocId> PuppetRoleDescription = [];
}
