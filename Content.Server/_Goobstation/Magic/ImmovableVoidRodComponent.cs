using Content.Shared.Damage;

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

    /// <summary>
    /// The type of damage to do to entities colliding with the rod.
    /// </summary>
    [DataField]
    public DamageSpecifier CollideDamage = new()
    {
        DamageDict = new()
        {
            {"Cold", 12.5},
        }
    };

}
