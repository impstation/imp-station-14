using Robust.Shared.Audio;

namespace Content.Shared._Impstation.Homunculi.Incubator.Components;

[RegisterComponent, AutoGenerateComponentState]
public sealed partial class IncubatorComponent : Component
{
    /// <summary>
    /// How much charge a single use expends.
    /// </summary>
    [DataField]
    public float ChargeUse = 200f;

    /// <summary>
    ///     How long to wait before finishing incubation.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan IncubationDuration = TimeSpan.FromSeconds(30);

    public EntityUid? PlayingStream;

    [DataField]
    public SoundSpecifier? LoopingSound = new SoundPathSpecifier("/Audio/Machines/spinning.ogg");

    [DataField]
    public string BeakerSlotId = "beaker_slot";

}
