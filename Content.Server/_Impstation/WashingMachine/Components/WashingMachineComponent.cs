using Content.Shared.DeviceLinking;
using Content.Shared.FixedPoint;
using Content.Shared.Item;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.WashingMachine.Components;
/// <summary>
/// Allows an entity with reagent storage and power to wash and dye items.
/// </summary>
[RegisterComponent]
public sealed partial class WashingMachineComponent : Component
{
    #region time

    [DataField("washTimeMultiplier"), ViewVariables(VVAccess.ReadWrite)]
    public float WashTimeMultiplier = 1;

    /// <summary>
    /// How long a single cycle lasts, in seconds.
    /// </summary>
    [DataField("washTimerRime"), ViewVariables(VVAccess.ReadWrite)]
    public uint WashTimerTime = 10;

    /// <summary>
    /// Tracks the elapsed time of the current wash timer.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan CurrentWashTimeEnd = TimeSpan.Zero;

    #endregion
    #region storage

    [DataField]
    public string ContainerId = "washingmachine_entity_container";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int Capacity = 10;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<ItemSizePrototype> MaxItemSize = "Normal";

    #endregion
    #region tank/power

    [ViewVariables]
    public bool Broken;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<SinkPortPrototype> OnPort = "On";

    /// <summary>
    /// Amount of water required to begin a cycle
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 WaterRequired = 150;

    /// <summary>
    /// Amount of cleaner reagent required to begin cleaning
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 CleanerRequired = 30;

    #endregion
    #region heat & damage

    [DataField("baseHeatMultiplier"), ViewVariables(VVAccess.ReadWrite)]
    public float BaseHeatMultiplier = 100;

    [DataField("objectHeatMultiplier"), ViewVariables(VVAccess.ReadWrite)]
    public float ObjectHeatMultiplier = 100;

    /// <summary>
    /// The max temperature that this washing machine can heat objects to.
    /// </summary>
    [DataField("temperatureUpperThreshold")]
    public float TemperatureUpperThreshold = 373.15f;

    [DataField("baseDamageMultiplier"), ViewVariables(VVAccess.ReadWrite)]
    public float BaseDamageMultiplier = 1;

    [DataField("objectDamageMultiplier"), ViewVariables(VVAccess.ReadWrite)]
    public float ObjectDamageMultiplier = 1;

    #endregion
    #region malfunction

    /// <summary>
    /// How frequently the washing machine can malfunction.
    /// </summary>
    [DataField]
    public float MalfunctionInterval = 1.0f;

    /// <summary>
    /// Chance of an explosion occurring when we wash dryer lint
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ExplosionChance = .05f;

    /// <summary>
    /// Chance of steam occurring when we wash dryer lint
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float SteamChance = .75f;

    [DataField("baseSteamOutput"), ViewVariables(VVAccess.ReadWrite)]
    public float BaseSteamOutput = .01f;

    [DataField("malfunctionSteamMultiplier"), ViewVariables(VVAccess.ReadWrite)]
    public float MalfunctionSteamMultiplier = 1000;

    #endregion
    #region  audio
    [DataField("startWashingSound")]
    public SoundSpecifier StartWashingSound = new SoundPathSpecifier("/Audio/Machines/microwave_start_beep.ogg");

    [DataField("cycleDoneSound")]
    public SoundSpecifier CycleDoneSound = new SoundPathSpecifier("/Audio/Machines/microwave_done_beep.ogg");

    [DataField("clickSound")]
    public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

    [DataField("ItemBreakSound")]
    public SoundSpecifier ItemBreakSound = new SoundPathSpecifier("/Audio/Effects/clang.ogg");

    public EntityUid? PlayingStream;

    [DataField("loopingSound")]
    public SoundSpecifier LoopingSound = new SoundPathSpecifier("/Audio/Machines/microwave_loop.ogg");
    #endregion
}

#region  events
public sealed class BeingWashedEvent : HandledEntityEventArgs
{
    public EntityUid WashingMachine;
    public EntityUid? User;

    public BeingWashedEvent(EntityUid washingMachine, EntityUid? user)
    {
        WashingMachine = washingMachine;
        User = user;
    }
}
#endregion
