namespace Content.Client._Impstation.Anchor;

[RegisterComponent]
[Access(typeof(AnchorVisualizerSystem))]
public sealed partial class AnchorVisualsComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? StateAnchored = "anchored";

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? StateUnanchored = "unanchored";
}
