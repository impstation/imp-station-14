namespace Content.Shared._Impstation.BloodlessChimp.Components;

[RegisterComponent]
[AutoGenerateComponentState]
public sealed partial class FearRadiusComponent : Component
{
    [DataField("radius")]
    public float Radius = 5f;

    [AutoNetworkedField]
    public EntityUid? Target;
}
