namespace Content.Server._Impstation.Gravity;

[RegisterComponent]
[Access(typeof(ZeroGravityAreaSystem))]
public sealed partial class ZeroGravityAreaComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public HashSet<Entity<IsInZeroGravityAreaComponent>> AffectedEntities = new();

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string Fixture = "antiGravity";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled = true;
}
