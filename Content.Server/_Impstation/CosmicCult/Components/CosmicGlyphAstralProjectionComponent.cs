using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.CosmicCult.Components;

[RegisterComponent]
public sealed partial class CosmicGlyphAstralProjectionComponent : Component
{
    [DataField]
    public EntProtoId SpawnProjection = "MobCosmicAstralProjection";

    /// <summary>
    /// The duration of the astral projection
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan AstralDuration = TimeSpan.FromSeconds(12);
}
