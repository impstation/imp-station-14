namespace Content.Shared._Impstation.BloodlessChimp.Components;
[RegisterComponent]

public sealed partial class FearRadiusComponent :  Component
{
    [DataField("warnings")]
    public List<string> Warnings= new ();

    [DataField("cooldown")]
    public TimeSpan CooldownTime = TimeSpan.FromSeconds(5);

    public string FixtureID = "fearRadiusTarget";
}
