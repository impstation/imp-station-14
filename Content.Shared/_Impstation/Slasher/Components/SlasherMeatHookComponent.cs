using Content.Shared.FixedPoint;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Slasher.Components;

/// <summary>
/// Applied to slasher-specific meat hooks.
/// Extends the generic spike behavior with Slasher-only tuning for self-unhooking,
/// soul harvesting, passive stabilization, and hook light colors.
/// </summary>
[RegisterComponent]
public sealed partial class SlasherMeatHookComponent : Component
{
    /// <summary>
    /// Maximum range for placing a meathook from the performer.
    /// </summary>
    [DataField]
    public float PlacementRange { get; set; } = 1.5f;

    /// <summary>
    /// Channel duration required to place a meathook.
    /// </summary>
    [DataField]
    public TimeSpan PlacementDoAfterDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Time it takes for a hooked victim to unhook themselves from a slasher meat hook.
    /// </summary>
    [DataField]
    public TimeSpan SelfUnhookDelay { get; set; } = TimeSpan.FromMinutes(3);

    /// <summary>
    /// Time it takes for a slasher to harvest a soul fragment from a hooked victim.
    /// </summary>
    [DataField]
    public TimeSpan HarvestDelay { get; set; } = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Prototype spawned when a valid harvest is performed.
    /// </summary>
    [DataField]
    public EntProtoId SoulFragmentPrototype { get; set; } = "SlasherSoulFragment";

    /// <summary>
    /// How long a harvested victim is protected from being harvested again.
    /// </summary>
    [DataField]
    public TimeSpan ReharvestDelay { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// How often healing checks should run for currently hooked victims.
    /// </summary>
    [DataField]
    public TimeSpan HealInterval { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Amount of damage to heal each tick while hooked.
    /// </summary>
    [DataField]
    public FixedPoint2 HealPerTick { get; set; } = 10f;

    /// <summary>
    /// Target total damage floor to stop healing at.
    /// Lower values leave victims closer to critical condition instead of fully recovering.
    /// </summary>
    [DataField]
    public FixedPoint2 TargetDamage { get; set; } = 5f;

    /// <summary>
    /// Target blood percentage to maintain while hooked so victims stabilize instead of bleeding out and aren't stuck looking at the blood shake slider for 3 minutes+.
    /// </summary>
    [DataField]
    public float TargetBloodLevel { get; set; } = 0.91f;

    /// <summary>
    /// Point light color shown while a victim's soul is ripe for harvest.
    /// </summary>
    [DataField]
    public Color PendingHarvestLightColor { get; set; } = Color.FromHex("#a855f7");

    /// <summary>
    /// Point light color shown after the soul has been harvested and the victim is in reharvest lockout.
    /// </summary>
    [DataField]
    public Color HarvestedLightColor { get; set; } = Color.FromHex("#800020");

    /// <summary>
    /// Minimum distance from the active effigy required to place a meathook.
    /// </summary>
    [DataField]
    public float MinimumEffigyDistance { get; set; } = 25f;

}
