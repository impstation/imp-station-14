using Content.Server._Impstation.Heretic.StationEvents.Events;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;


namespace Content.Server._Impstation.Heretic.StationEvents.Components;

/// <summary>
/// Used an event that spawns an anomaly somewhere random on the map.
/// </summary>
[RegisterComponent, Access(typeof(HereticBookWaveRule))]
public sealed partial class HereticBookWaveRuleComponent : Component
{
    public readonly SoundSpecifier RiftSpawnSound = new SoundPathSpecifier("/Audio/_Goobstation/Heretic/Ambience/Antag/Heretic/heretic_gain.ogg");

    [DataField("realityTearPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string RealityTearPrototype = "RealityTear";

    [DataField("minBooks")]
    public int MinBooks = 1;

    [DataField("maxBooks")]
    public int MaxBooks = 2;
}
