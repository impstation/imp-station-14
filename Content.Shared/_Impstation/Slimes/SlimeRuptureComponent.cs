namespace Content.Shared._Impstation.Slimes;

/// <summary>
/// Causes mobs to take increased bloodloss per blood stack. BloodstreamSystem will check for this component and modify bleed bloodloss accordingly.
/// </summary>
[RegisterComponent]
public sealed partial class SlimeRuptureComponent : Component
{
    [DataField("bleedIncreaseMultiplier"), ViewVariables(VVAccess.ReadWrite)]
    public float BleedIncreaseMultiplier = 1.33f;
}
