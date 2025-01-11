namespace Content.Server._Impstation.Cosmiccult.Components;
[RegisterComponent]

public sealed partial class CosmicEntropyMoteComponent : Component
{
    [DataField("entropy"), ViewVariables(VVAccess.ReadWrite)]
    public int Entropy = 1;
}
