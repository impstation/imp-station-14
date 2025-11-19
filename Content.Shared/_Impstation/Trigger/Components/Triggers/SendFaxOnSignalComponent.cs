using Content.Shared.DeviceLinking;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Trigger.Components.Triggers;

/// <summary>
/// This is used in fax machines to send a fax when a signal is received.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SendFaxOnSignalComponent : Component
{
    /// <summary>
    /// The sink port prototype we can connect devices to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<SinkPortPrototype> Port = "SendFax";
}
