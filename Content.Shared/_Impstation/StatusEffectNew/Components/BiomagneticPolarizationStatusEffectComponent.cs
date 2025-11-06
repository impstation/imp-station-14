using Content.Shared.Explosion;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.StatusEffectNew;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
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
    [DataField("polarization: N=T S=F"), AutoNetworkedField]
    public bool Polarization;
    /// <summary>
    /// This should probably never be changed.
    /// </summary>
    [DataField]
    public float NorthChance = 0.5f;

    [DataField]
    public Color NorthColor = Color.Red;
    [DataField]
    public Color SouthColor = Color.Blue;

    /// <summary>
    /// Current magnetism strength. Determines the strength of the effects of collisions.
    /// </summary>
    [DataField]
    public float CurrentStrength = 10f;
    /// <summary>
    /// Cap above which strength values are clamped.
    /// </summary>
    [DataField]
    public float StrengthCap = 40f;
    /// <summary>
    /// If either entity in a collision is within CapEffectMargin of StrengthCap,
    /// we'll add some extra effects to a magnetic blowout to make it more dangerous.
    /// </summary>
    [DataField]
    public float CapEffectMargin = 5f;
    /// <summary>
    /// The range in tiles in which cap effects (similar to revenant spookies) will occur
    /// </summary>
    [DataField]
    public float CapEffectRange = 3f;
    [DataField]
    public float CapEffectChance = 0.5f;

    [DataField]
    public float MinDecayRate = 0.01f;
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
    [DataField]
    public float LightningRange = 5f;
    [DataField]
    public (int, int) LightningArcsMinMax = (1, 3);
    [DataField]
    public float LightningCapMult = 2f;

    /// <summary>
    /// The proto of explosion that happens when two opposite fields touch.
    /// </summary>
    [DataField]
    public ProtoId<ExplosionPrototype> ExplosionPrototype = "Electrical";
    /// <summary>
    /// Multiplier applied to explosion strength when two opposite-polarity entities collide.
    /// </summary>
    [DataField]
    public float ExplosionStrengthMult = 2f;
    /// <summary>
    /// Multiplier applied to explosion strength when either entity in a collision is within range of the strength cap.
    /// </summary>
    [DataField]
    public float CapExplosionMult = 5f;

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

    [DataField]
    public SoundSpecifier CapExplosionSound = new SoundPathSpecifier("/Audio/_Impstation/Effects/lightning_strike.ogg");

    public Entity<PhysicsComponent?>? StatusOwner = null;

    /// <summary>
    /// when this gets marked true, the status effect is marked for disposal. this is to ensure parity between colliders
    /// </summary>
    public bool Expired;

    /// <summary>
    /// stores the last tile this entity was standing on. used for calculating movement between tiles
    /// </summary>
    public TileRef LastTile = TileRef.Zero;

    [DataField, AutoNetworkedField]
    public bool Capped;
    [DataField, AutoNetworkedField]
    public bool LastCapped;
}

[Serializable, NetSerializable]
public enum BiomagneticPolarizationLayers : byte
{
    Capped
}
