using Content.Shared.DeviceLinking;
using Content.Shared.Item;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.WashingMachine.Components;
/// <summary>
/// Allows an entity with reagent storage and power to wash and dye items.
/// </summary>
[RegisterComponent]
public sealed partial class WashingMachineComponent : Component
{
    [DataField("washTimeMultiplier"), ViewVariables(VVAccess.ReadWrite)]
    public float WashTimeMultiplier = 1;

    [DataField("baseHeatMultiplier"), ViewVariables(VVAccess.ReadWrite)]
    public float BaseHeatMultiplier = 100;

    [DataField("objectHeatMultiplier"), ViewVariables(VVAccess.ReadWrite)]
    public float ObjectHeatMultiplier = 100;

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

    [ViewVariables]
    public bool Broken;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<SinkPortPrototype> OnPort = "On";

    /// <summary>
    /// This is a fixed offset of 5.
    /// The cook times for all recipes should be divisible by 5,with a minimum of 1 second.
    /// For right now, I don't think any recipe cook time should be greater than 60 seconds.
    /// </summary>
    [DataField("currentCookTimerTime"), ViewVariables(VVAccess.ReadWrite)]
    public uint CurrentCookTimerTime = 0;

    /// <summary>
    /// Tracks the elapsed time of the current cook timer.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan CurrentCookTimeEnd = TimeSpan.Zero;

    /// <summary>
    /// The maximum number of seconds a microwave can be set to.
    /// This is currently only used for validation and the client does not check this.
    /// </summary>
    [DataField("maxCookTime"), ViewVariables(VVAccess.ReadWrite)]
    public uint MaxCookTime = 30;

    /// <summary>
    /// The max temperature that this washing machine can heat objects to.
    /// </summary>
    [DataField("temperatureUpperThreshold")]
    public float TemperatureUpperThreshold = 373.15f;

    public int CurrentCookTimeButtonIndex;

    public Container Contents = default!;

    [DataField]
    public string ContainerId = "washingmachine_entity_container";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int Capacity = 10;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<ItemSizePrototype> MaxItemSize = "Normal";

    /// <summary>
    /// How frequently the microwave can malfunction.
    /// </summary>
    [DataField]
    public float MalfunctionInterval = 1.0f;
}

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
