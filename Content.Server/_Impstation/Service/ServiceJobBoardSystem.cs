using Content.Server.Station.Systems;
using Content.Shared._Impstation.Service;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Service;

public sealed class ServiceJobBoardSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ServiceJobBoardConsoleComponent, BoundUIOpenedEvent>(OnBUIOpened);
    }

    /// <summary>
    ///     Gets a list of random jobs.
    /// </summary>
    public List<ProtoId<ServiceJobPrototype>> GetJobs(Entity<ServiceJobsDataComponent> ent)
    {
        // already generated 'em
        if (ent.Comp.StationJobs != null)
            return ent.Comp.StationJobs;

        List<ProtoId<ServiceJobPrototype>> outJobs = [];

        foreach (var job in _prototypeManager.EnumeratePrototypes<ServiceJobPrototype>())
        {
            // TODO: shuffle these so we get a random order each time
            if (outJobs.Contains(job))
                continue;

            outJobs.Add(job);
        }
        return outJobs;
    }

    private void OnBUIOpened(Entity<ServiceJobBoardConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (args.UiKey is not ServiceJobBoardUiKey.Key)
            return;

        if (_station.GetOwningStation(ent.Owner) is not { } station ||
            !TryComp<ServiceJobsDataComponent>(station, out var jobData))
            return;

        UpdateUi(ent, (station, jobData));
    }

    private void UpdateUi(Entity<ServiceJobBoardConsoleComponent> ent, Entity<ServiceJobsDataComponent> stationEnt)
    {
        var state = new ServiceJobBoardConsoleState(
            GetJobs(stationEnt));

        _ui.SetUiState(ent.Owner, ServiceJobBoardUiKey.Key, state);
    }
}
