using Content.Shared._Impstation.Mime;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Mime;

/// <summary>
/// This component stores the possible contents of the backpack,
/// which can be selected via the interface.
/// </summary>
[RegisterComponent, Access(typeof(MimeGearMenuSystem))]
public sealed partial class MimeGearMenuComponent : Component
{
    /// <summary>
    /// List of sets available for selection
    /// </summary>
    [DataField]
    public List<ProtoId<MimeGearMenuSetPrototype>> PossibleSets = new();

    [DataField]
    public List<int> SelectedSets = new();

    /// <summary>
    /// Max number of sets you can select.
    /// </summary>
    [DataField]
    public int MaxSelectedSets = 1;
}
