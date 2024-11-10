using Content.Shared._Impstation.Gravity;

namespace Content.Server._Impstation.Gravity;

[RegisterComponent]
[Access(typeof(ZeroGravityAreaSystem))]
public sealed partial class ZeroGravityAreaComponent : SharedZeroGravityAreaComponent
{
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public HashSet<Entity<IsInZeroGravityAreaComponent>> AffectedEntities = new();
}
