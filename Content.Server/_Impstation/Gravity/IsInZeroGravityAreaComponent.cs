using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Server._Impstation.Gravity;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ZeroGravityAreaSystem))]
public sealed partial class IsInZeroGravityAreaComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public HashSet<Entity<ZeroGravityAreaComponent>> AffectingAreas = new();
}
