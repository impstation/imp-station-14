using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Impstation.BloodlessChimp.Components;

[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class FearRadiusComponent : Component
{
    [DataField("radius")]
    public float Radius = 5f;

    [DataField("warnings")]
    public List<string> Warnings= new ();

    [DataField("cooldown")]
    public TimeSpan CooldownTime = TimeSpan.FromSeconds(5);

    [DataField( customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan CurrentCooldown = TimeSpan.Zero;

    [AutoNetworkedField]
    public EntityUid? Target;
}
