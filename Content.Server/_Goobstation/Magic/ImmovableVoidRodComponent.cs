using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Magic;

/// <summary>
/// Special non-damaging rod for heretics' void blast ability.
/// </summary>
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

    /// <summary>
    /// Amount of damage done when rod collides with an entity, default 12.5.
    /// Assuming no resist, this puts the target in range to be crit by void blade in 5 hits
    /// </summary>
    [DataField]
    public float DamageAmount = 12.5f;
}
