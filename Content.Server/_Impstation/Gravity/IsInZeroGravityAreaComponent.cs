using Content.Shared._Impstation.Gravity;
using Robust.Shared.GameStates;

namespace Content.Server._Impstation.Gravity;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedZeroGravityAreaSystem))]
public sealed partial class IsInZeroGravityAreaComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public HashSet<Entity<ZeroGravityAreaComponent>> AffectingAreas = new();
}
