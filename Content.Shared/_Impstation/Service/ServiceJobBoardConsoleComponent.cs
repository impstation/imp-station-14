using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Impstation.Service;

/// <summary>
///     Used to view the service job board ui
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ServiceJobBoardConsoleComponent : Component { }

[Serializable, NetSerializable]
public sealed class ServiceJobBoardConsoleState : BoundUserInterfaceState
{
    public List<ProtoId<ServiceJobPrototype>> AvailableJobs;

    public ServiceJobBoardConsoleState(List<ProtoId<ServiceJobPrototype>> availableJobs)
    {
        AvailableJobs = availableJobs;
    }
}

[Serializable, NetSerializable]
public sealed class JobBoardPrintLabelMessage : BoundUserInterfaceMessage
{
    public string JobId;

    public JobBoardPrintLabelMessage(string jobId)
    {
        JobId = jobId;
    }
}

[Serializable, NetSerializable]
public enum ServiceJobBoardUiKey : byte
{
    Key
}
