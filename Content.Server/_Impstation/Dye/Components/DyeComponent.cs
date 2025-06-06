namespace Content.Server._Impstation.Dye.Components;

[RegisterComponent]
/// <summary>
/// Signifies that this entity can be washed in a washing machine to dye eligible items.
/// </summary>
public sealed partial class DyeComponent : Component
{
    [DataField("color")]
    public string Color;
}
