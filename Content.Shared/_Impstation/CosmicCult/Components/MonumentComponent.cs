using Content.Shared._Impstation.CosmicCult.Prototypes;
using Content.Shared.Radio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.CosmicCult.Components;
[NetworkedComponent, RegisterComponent, AutoGenerateComponentState]
public sealed partial class MonumentComponent : Component
{
    [NonSerialized] public static int LayerMask = 777;
    [DataField] public List<ProtoId<InfluencePrototype>> UnlockedInfluences = [];
    [DataField] public ProtoId<GlyphPrototype> SelectedGlyph;
    [DataField] public int AvailableEntropy;
    [DataField] public int TotalEntropy;
    [DataField] public int EntropyUntilNextStage;
    [DataField] public int CrewToConvertNextStage;
    [DataField] public float PercentageComplete;
    [DataField] public bool FinaleReady = false;
    [DataField] public bool Occupied = false;
    [DataField, AutoNetworkedField] public TimeSpan TransformTime = TimeSpan.FromSeconds(2.8);
}

[Serializable, NetSerializable]
public sealed class GlyphSelectedMessage(ProtoId<GlyphPrototype> glyphProtoId) : BoundUserInterfaceMessage
{
    public ProtoId<GlyphPrototype> GlyphProtoId = glyphProtoId;
}

[Serializable, NetSerializable]
public sealed class InfluenceSelectedMessage(ProtoId<InfluencePrototype> influenceProtoId) : BoundUserInterfaceMessage
{
    public ProtoId<InfluencePrototype> InfluenceProtoId = influenceProtoId;
}
[Serializable, NetSerializable]
public enum MonumentVisuals : byte
{
    Monument,
    Transforming,
    FinaleReached,
    Tier3
}

[Serializable, NetSerializable]
public enum MonumentVisualLayers : byte
{
    MonumentLayer,
    TransformLayer,
    FinaleLayer
}
