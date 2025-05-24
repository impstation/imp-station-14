using Content.Server._Impstation.WashingMachine.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.DeviceLinking;
using Content.Shared.FixedPoint;
using Content.Shared.Item;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.WashingMachine.Components;
/// <summary>
/// Allows an entity with reagent storage and power to wash and dye items.
/// </summary>
[RegisterComponent, Access(typeof(WashingMachineSystem))]
public sealed partial class WashingMachineComponent : Component
{
    #region time

    /// <summary>
    /// How long a single cycle lasts, in seconds.
    /// </summary>
    [DataField]
    public uint WashTimerTime = 10;

    /// <summary>
    /// Tracks the elapsed time of the current wash timer.
    /// </summary>
    [DataField]
    public TimeSpan CurrentWashTimeEnd = TimeSpan.Zero;

    /// <summary>
    /// How long this washing machine will apply the dizzy status effect to eligible entities.
    /// </summary>
    [DataField]
    public float DizzyMultiplier = 5;

    #endregion
    #region storage

    [DataField]
    public string ContainerId = "washingmachine_entity_container";

    [DataField]
    public int Capacity = 10;

    [DataField]
    public ProtoId<ItemSizePrototype> MaxItemSize = "Normal";

    #endregion
    #region tank/power

    [ViewVariables]
    public bool Broken;

    [ViewVariables]
    public bool Bloody;

    [DataField]
    public ProtoId<SinkPortPrototype> OnPort = "On";

    /// <summary>
    /// Amount of water required to begin a cycle
    /// </summary>
    [DataField]
    public FixedPoint2 WaterRequired = 150;

    /// <summary>
    /// Amount of cleaner reagent required to begin cleaning
    /// </summary>
    [DataField]
    public FixedPoint2 CleanerRequired = 30;

    #endregion
    #region heat & damage

    [DataField]
    public float BaseHeatMultiplier = 100;

    [DataField]
    public float ObjectHeatMultiplier = 100;

    // TODO: make heating of components in a container its own component, and have microwave use it
    /// <summary>
    /// The max temperature that this washing machine can heat objects to.
    /// </summary>
    [DataField]
    public float TemperatureUpperThreshold = 373.15f;

    /// <summary>
    /// How much damage to apply to the entity inside
    /// </summary>
    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new()
        {
            { "Blunt", 1 },
        }
    };

    #endregion
    #region malfunction

    /// <summary>
    /// Entity prototype that has a chance to be spawned as dryer lint.
    /// </summary>
    [DataField]
    public EntProtoId LintPrototype = "WashingLintTest";

    /// <summary>
    /// Chance per cycle of dryer lint spawning.
    /// </summary>
    [DataField]
    public float LintChance = 0.1f;

    /// <summary>
    /// How frequently in seconds the washing machine can malfunction (can roll multiple times).
    /// </summary>
    [DataField]
    public float MalfunctionInterval = 1.0f;

    /// <summary>
    /// Chance of an explosion when a malfunction is rolled.
    /// </summary>
    [DataField]
    public float ExplosionChance = .05f;

    /// <summary>
    /// Chance of steam occurring when a malfunction is rolled.
    /// </summary>
    [DataField]
    public float SteamChance = .75f;

    [DataField]
    public float BaseSteamOutput = .01f;

    [DataField]
    public float MalfunctionSteamMultiplier = 1000;

    /// <summary>
    /// Chance of a melee weapon in the wash damaging the machine.
    /// </summary>
    [DataField]
    public float WeaponHitChance = 0.05f;

    #endregion
    #region  audio
    [DataField]
    public SoundSpecifier StartWashingSound = new SoundPathSpecifier("/Audio/Machines/microwave_start_beep.ogg");

    [DataField]
    public SoundSpecifier CycleDoneSound = new SoundPathSpecifier("/Audio/Machines/microwave_done_beep.ogg");

    [DataField]
    public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

    [DataField]
    public SoundSpecifier ItemBreakSound = new SoundPathSpecifier("/Audio/Effects/clang.ogg");

    public EntityUid? PlayingStream;

    [DataField]
    public SoundSpecifier LoopingSound = new SoundPathSpecifier("/Audio/Machines/microwave_loop.ogg");
    #endregion
}

#region  events
public sealed class ActivelyBeingWashedEvent : HandledEntityEventArgs
{
    public EntityUid WashingMachine;
    public EntityUid? User;

    public ActivelyBeingWashedEvent(EntityUid washingMachine)
    {
        WashingMachine = washingMachine;
    }
}

/// <summary>
/// Raised by <see cref="WashingMachineSystem"/> on a washing machine when washing is complete.
/// </summary>
public sealed class WashEndEvent : HandledEntityEventArgs
{
    public EntityUid Storage;

    public WashEndEvent(EntityUid storage)
    {
        Storage = storage;
    }
}
#endregion
