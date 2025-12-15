using Content.Shared.Radio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Service;

/// <summary>
///     Used to view the service job board ui
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ServiceJobBoardConsoleComponent : Component
{
    /// <summary>
    ///     Radio channel that job selected messages are announced on.
    /// </summary>
    [DataField]
    public ProtoId<RadioChannelPrototype> AnnounceChannel = "Service";
}

[Serializable, NetSerializable]
public sealed class ServiceJobBoardConsoleState : BoundUserInterfaceState
{
    public List<ProtoId<ServiceJobPrototype>> AvailableJobs;
    public ProtoId<ServiceJobPrototype>? ActiveJob;
    public TimeSpan? EndTime;

    public ServiceJobBoardConsoleState(List<ProtoId<ServiceJobPrototype>> availableJobs,
    ProtoId<ServiceJobPrototype>? activeJob,
    TimeSpan? endTime)
    {
        AvailableJobs = availableJobs;
        ActiveJob = activeJob;
        EndTime = endTime;
    }
}

[Serializable, NetSerializable]
public sealed class ServiceJobBoardSelectMessage : BoundUserInterfaceMessage
{
    public string JobId;

    public ServiceJobBoardSelectMessage(string jobId)
    {
        JobId = jobId;
    }
}

[Serializable, NetSerializable]
public enum ServiceJobBoardUiKey : byte
{
    Key
}
