using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Impstation.Homunculi.Components;

[RegisterComponent]
public sealed partial class HomunculiTypeComponent : Component
{
    [DataField(customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string HomunculiType = "HomunculusHuman";

    [DataField(customTypeSerializer:typeof(PrototypeIdDictionarySerializer<FixedPoint2, ReagentPrototype>))]
    public Dictionary<string, FixedPoint2> Recipe = new();
}
