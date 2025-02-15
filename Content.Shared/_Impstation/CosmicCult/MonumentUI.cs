using Content.Shared._Impstation.CosmicCult.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Cosmiccult;
// Content.Shared/_Impstation/CosmicCult/MonumentUI.cs
[Serializable, NetSerializable]
public enum MonumentKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class MonumentBuiState : BoundUserInterfaceState
{
    public int AvailableEntropy;
    public int EntropyUntilNextStage;
    public int CrewToConvertUntilNextStage;
    public float PercentageComplete;
    public ProtoId<GlyphPrototype> SelectedGlyph;
    public List<ProtoId<InfluencePrototype>> UnlockedInfluences;
    public List<ProtoId<GlyphPrototype>> UnlockedGlyphs;
    public MonumentBuiState(int availableEntropy, int entropyUntilNextStage, int crewToConvertUntilNextStage, float percentageComplete, ProtoId<GlyphPrototype> selectedGlyph, List<ProtoId<InfluencePrototype>> unlockedInfluences, List<ProtoId<GlyphPrototype>> unlockedGlyphs)
    {
        AvailableEntropy = availableEntropy;
        EntropyUntilNextStage = entropyUntilNextStage;
        CrewToConvertUntilNextStage = crewToConvertUntilNextStage;
        PercentageComplete = percentageComplete;
        SelectedGlyph = selectedGlyph;
        UnlockedInfluences = unlockedInfluences;
        UnlockedGlyphs = unlockedGlyphs;
    }
}
