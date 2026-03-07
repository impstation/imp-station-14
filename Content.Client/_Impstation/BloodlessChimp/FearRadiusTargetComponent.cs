using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Impstation.BloodlessChimp.Components;

[RegisterComponent]
public sealed partial class FearRadiusTargetComponent : Component
{

    [DataField("warnings")]
    public List<string> Warnings= new ();

    [DataField("cooldown")]
    public TimeSpan CooldownTime = TimeSpan.FromSeconds(5);

    [DataField( customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan CurrentCooldown = TimeSpan.Zero;

    public string FixtureID = "fearRadius";
}
