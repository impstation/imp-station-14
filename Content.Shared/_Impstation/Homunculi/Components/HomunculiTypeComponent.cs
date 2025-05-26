using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared._Impstation.Homunculi.Components;

[RegisterComponent]
public sealed partial class HomunculiTypeComponent : Component
{
    [DataField(customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>), required:true)]
    public string HomunculiType;

    [DataField(customTypeSerializer:typeof(PrototypeIdDictionarySerializer<FixedPoint2, ReagentPrototype>))]
    public Dictionary<string, FixedPoint2> Recipe = new();
}
