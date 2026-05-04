using Content.Shared.Spreader;
using Robust.Shared.Prototypes;

namespace Content.Server.Spreader;

/// <summary>
/// Entity capable of becoming cloning and replicating itself to adjacent edges. See <see cref="SpreaderSystem"/>
/// </summary>
[RegisterComponent, Access(typeof(SpreaderSystem))]
public sealed partial class EdgeSpreaderComponent : Component
{
    [DataField(required: true)]
    public ProtoId<EdgeSpreaderPrototype> Id;

    /// <summary>
    /// Imp
    /// Allows independent updating of each spreader entity
    /// </summary>
    [DataField]
    public bool IndependentUpdate = false;
}
