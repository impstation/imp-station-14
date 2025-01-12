namespace Content.Server._Impstation.CosmicCult.Components;
[RegisterComponent]

public sealed partial class CosmicEntropyMoteComponent : Component
{
    [DataField("entropy"), ViewVariables(VVAccess.ReadWrite)]
    public int Entropy = 1;
}
