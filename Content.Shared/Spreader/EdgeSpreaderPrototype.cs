using Robust.Shared.Prototypes;

namespace Content.Shared.Spreader;

/// <summary>
/// Adds this node group to <see cref="Content.Server.Spreader.SpreaderSystem"/> for tick updates.
/// </summary>
[Prototype]
public sealed partial class EdgeSpreaderPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = string.Empty;
    [DataField(required:true)] public int UpdatesPerSecond;

    /// <summary>
    /// If true, this spreader can't spread onto spaced tiles like lattice.
    /// </summary>
    [DataField]
    public bool PreventSpreadOnSpaced = true;

    /// <summary>
    /// Controls whether the spreader can spread to airtight stuff.
    /// </summary>
    [DataField]
    public bool PreventSpreadOnAirtight = true;

    /// <summary>
    /// Controls whether the spreader can ignore space check & see space as a valid tile.
    /// </summary>
    [DataField]
    public bool PreventSpreadOnSpace = true;
}
