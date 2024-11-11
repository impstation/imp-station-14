using Content.Shared._Impstation.Gravity;
using Robust.Shared.GameStates;
using Serilog;

namespace Content.Server._Impstation.Gravity;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ZeroGravityAreaSystem))]
public sealed partial class ZeroGravityAreaComponent : SharedZeroGravityAreaComponent
{
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public HashSet<Entity<IsInZeroGravityAreaComponent>> AffectedEntities = new();
}
