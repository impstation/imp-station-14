using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.CosmicCult.Components;

[RegisterComponent]
public sealed partial class CosmicGlyphConversionComponent : Component
{
    /// <summary>
    ///     The search range for finding conversion targets.
    /// </summary>
    [DataField]
    public float ConversionRange = 0.5f;

    /// <summary>
    ///     Healing applied on conversion.
    /// </summary>
    [DataField]
    public DamageSpecifier ConversionHeal = new()
    {
        DamageDict = new()
        {
            ["Brute"] = -50,
            ["Burn"] = -50
        }
    };

    [DataField]
    public EntProtoId ConvertVFX = "CosmicConversionVFX";

    [DataField]
    public SoundSpecifier ConvertSFX = new SoundPathSpecifier("/Audio/_Impstation/CosmicCult/cult_conversion.ogg");

}
