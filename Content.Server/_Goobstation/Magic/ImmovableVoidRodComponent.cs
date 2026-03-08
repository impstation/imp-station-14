using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Magic;

[RegisterComponent]
public sealed partial class ImmovableVoidRodComponent : Component
{
    [DataField]
    public TimeSpan Lifetime = TimeSpan.FromSeconds(1f);

    public float Accumulator = 0f;

    [DataField]
    public string SnowWallPrototype = "WallIce";

    [DataField]
    public string IceTilePrototype = "FloorAstroIce";

    [DataField]
    public ProtoId<DamageTypePrototype> DamageType = "Cold";

    [DataField]
    public float DamageAmount = 12.5f; //assuming no resist, puts the target in range to be killed by void blade in 5 hits
}
