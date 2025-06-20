namespace Content.Shared._Impstation.EntityEffects.Effects;

/// <summary>
/// Sets which sprite RSI is used for displaying dizzy visuals
/// </summary>
[RegisterComponent]
public sealed partial class DizzyVisualsComponent : Component
{
    [DataField]
    public string? State;

    [DataField]
    public string? Sprite;
}
