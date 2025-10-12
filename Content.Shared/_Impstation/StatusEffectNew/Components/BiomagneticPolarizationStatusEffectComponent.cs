using Content.Shared.Explosion;
using Content.Shared.Whitelist;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.StatusEffectNew;

[RegisterComponent, AutoGenerateComponentState]
public sealed partial class BiomagneticPolarizationStatusEffectComponent : Component
{
    /// <summary>
    /// Time in seconds between strength updates.
    /// </summary>
    [DataField]
    public TimeSpan UpdateTime = TimeSpan.FromSeconds(1);
    public TimeSpan NextUpdate = TimeSpan.Zero;

    /// <summary>
    /// Time in seconds the user should wait before triggering an effect again.
    /// </summary>
    [DataField]
    public TimeSpan TriggerCooldown = TimeSpan.FromSeconds(3);
    public TimeSpan CooldownEnd = TimeSpan.Zero;

    /// <summary>
    /// IMPORTANT: Polarization is a bool, but true and false don't really apply.
    /// TRUE = NORTH
    /// FALSE = SOUTH
    /// </summary>
    [DataField("polarization: N=T S=F")]
    public bool Polarization;

    [DataField]
    public Color NorthColor = Color.Red;
    [DataField]
    public Color SouthColor = Color.Blue;

    /// <summary>
    /// Determines the strength of repulsion, and by extension the brightness of the glow.
    /// </summary>
    [DataField]
    public float CurrentStrength = 10f;

    [DataField]
    public float MinDecayRate = 0.03f;
    [DataField]
    public float MaxDecayRate = 0.05f;
    /// <summary>
    /// The current actual decay rate, once determined randomly.
    /// Keep in mind when setting decay rate settings that they're subtracted once per second,
    /// so at a default strength of 10, a decay rate of 0.1 will decay to 0 in 100 seconds.
    /// </summary>
    [DataField]
    public float RealDecayRate = 0.0f;

    /// <summary>
    /// Multiplier applied to CurrentStrength before applying to the pointlight strength.
    /// </summary>
    [DataField]
    public float StrLightMult = 5.0f;
    /// <summary>
    /// Multiplier applied to throw strength when two same-polarity entities collide.
    /// </summary>
    [DataField]
    public float ThrowStrengthMult = 0.8f;

    /// <summary>
    /// The proto of lightning that we shoot when two opposite fields touch.
    /// </summary>
    [DataField]
    public EntProtoId LightningPrototype = "Lightning";

    /// <summary>
    /// The proto of explosion that happens when two opposite fields touch.
    /// </summary>
    [DataField]
    public ProtoId<ExplosionPrototype> ExplosionPrototype = "Cryo";
    /// <summary>
    /// Multiplier applied to explosion strength when two opposite-polarity entities collide.
    /// </summary>
    [DataField]
    public float ExplosionStrengthMult = 2f;

    /// <summary>
    /// Whitelist of entities that count towards Strength increase when moving onto a tile which contains them.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist StaticElectricityProviders;

    [DataField]
    public float StrProvidedByStatic = 0.1f;

    /// <summary>
    /// Copied this from MobCollisionSystem.
    /// If MobCollisionSystem ever gets updated to use its own fixtures, probably oughta change this.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string FixtureId = "flammable";

    public Entity<PhysicsComponent>? StatusOwner = null;

    /// <summary>
    /// when this gets marked true, the status effect is marked for disposal. this is to ensure parity between colliders
    /// </summary>
    public bool Expired;

    /// <summary>
    /// stores the last tile this entity was standing on. used for calculating movement between tiles
    /// </summary>
    public TileRef LastTile = TileRef.Zero;
}
