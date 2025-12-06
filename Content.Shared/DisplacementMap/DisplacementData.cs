using Robust.Shared.Serialization;

namespace Content.Shared.DisplacementMap;

[DataDefinition, Serializable, NetSerializable]
public sealed partial class DisplacementData
{
    /// <summary>
    /// allows you to attach different maps for layers of different sizes.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<int, PrototypeLayerData> SizeMaps = new();

    [DataField]
    public string? ShaderOverride = "DisplacedDraw";

    [DataField]
    public string ShaderOverrideUnshaded = "DisplacedStencilDrawUnshaded"; // imp

    [DataField]
    public string ShaderOverrideAdditive = "DisplacedStencilDrawAdditive"; // imp

    /// <summary>
    ///     When true, will apply this displacement to clothing regardless of whether the clothing layer is specified to be for a particular species.
    /// </summary>
    [DataField]
    public bool IgnoreSpeciesClothing = false;
}
