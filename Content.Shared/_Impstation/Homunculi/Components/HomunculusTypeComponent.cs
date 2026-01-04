using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Impstation.Homunculi.Components;

[RegisterComponent]
public sealed partial class HomunculusTypeComponent : Component
{
    [DataField]
    public EntProtoId HomunculusType = "HomunculusHuman";

    [DataField]
    public Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> Recipe = new();
}
