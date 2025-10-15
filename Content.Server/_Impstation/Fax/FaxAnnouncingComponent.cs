using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Fax;

/// <summary>
/// Causes this entity to announce onto the provided channels when it receives a fax.
/// </summary>
[RegisterComponent]
public sealed partial class FaxAnnouncingComponent : Component
{
    /// <summary>
    /// Radio channels to broadcast to when a fax is received.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<RadioChannelPrototype>> Channels = new();
}
