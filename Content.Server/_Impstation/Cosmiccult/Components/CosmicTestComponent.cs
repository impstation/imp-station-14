namespace Content.Server._Impstation.Cosmiccult.Components;

[RegisterComponent]
public sealed partial class CosmicTestComponent : Component
{
    public float UpdateTimer = 0f;
    [DataField] public float UpdateDelay = 1.5f;
    [DataField] public float Range = 7f;
}
