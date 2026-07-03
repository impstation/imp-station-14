using Content.Shared.Damage;
using Robust.Shared.Audio;

namespace Content.Shared._Impstation.Slasher.Components;

/// <summary>
/// Placed on the Rend action entity.
/// Controls the damage, timing, and sounds used by Rend.
/// </summary>
[RegisterComponent]
public sealed partial class SlasherRendActionComponent : Component
{
    /// <summary>
    /// Windup time before the structure is torn apart.
    /// </summary>
    [DataField]
    public float RendDelay { get; set; } = 5f;

    /// <summary>
    /// Damage applied to targeted structures. Should be large enough to destroy any wall, door, or window.
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier RendDamage { get; set; } = default!;

    /// <summary>
    /// Maximum distance the user can move from the target while channeling Rend.
    /// </summary>
    [DataField]
    public float DistanceThreshold { get; set; } = 1.5f;

    /// <summary>
    /// Sound played when Rend begins winding up on an obstacle.
    /// </summary>
    [DataField]
    public SoundSpecifier RendStartSound { get; set; } = new SoundPathSpecifier("/Audio/Machines/airlock_creaking.ogg");

    /// <summary>
    /// Sound played when Rend finishes and destroys the obstacle.
    /// </summary>
    [DataField]
    public SoundSpecifier RendCompleteSound { get; set; } = new SoundCollectionSpecifier("MetalCrunch");

    /// <summary>
    /// Sound played when Rend finishes and destroys a window target.
    /// </summary>
    [DataField]
    public SoundSpecifier WindowRendCompleteSound { get; set; } = new SoundCollectionSpecifier("GlassBreak");

    /// <summary>
    /// Tag identifying wall-like structures that can be rend targeted.
    /// </summary>
    [DataField]
    public string WallTag { get; set; } = "Wall";

    /// <summary>
    /// Tag identifying window-like structures that can be rend targeted.
    /// </summary>
    [DataField]
    public string WindowTag { get; set; } = "Window";
}
