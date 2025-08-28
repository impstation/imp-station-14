using Content.Server.Radio.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared._Impstation.Service;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Service;

/// <summary>
///     System handling service job board.
///     TLDR: Console handles UI interactions,
///     all the actual data is stored by the station.
/// </summary>
public sealed class ServiceJobBoardSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly RadioSystem _radio = default!;

    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ServiceJobBoardConsoleComponent, BoundUIOpenedEvent>(OnBUIOpened);
        Subs.BuiEvents<ServiceJobBoardConsoleComponent>(ServiceJobBoardUiKey.Key,
            subs =>
            {
                subs.Event<ServiceJobBoardSelectMessage>(OnSelectMessage);
            });
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

    private void OnSelectMessage(Entity<ServiceJobBoardConsoleComponent> ent, ref ServiceJobBoardSelectMessage args)
    {
        if (_station.GetOwningStation(ent) is not { } station ||
            !TryComp<ServiceJobsDataComponent>(station, out var jobData))
            return;

        if (!_prototypeManager.TryIndex<ServiceJobPrototype>(args.JobId, out var job))
            return;

        if (jobData.StationJobs != null &&
            !jobData.StationJobs.Contains(job)) // TODO check this actually works
            return;

        jobData.ActiveJob = job;

        // disable shit
        // setup announce for comms console

        var message = Loc.GetString(job.ServiceAnnouncement);
        _radio.SendRadioMessage(ent, message, ent.Comp.AnnounceChannel, ent, false);

        // we need to update the state of all computers, not just the one in use
        var query = EntityQueryEnumerator<ServiceJobBoardConsoleComponent>();
        while (query.MoveNext(out var uid, out var console))
        {
            if (_station.GetOwningStation(ent.Owner) is not { } queryStation ||
                !TryComp<ServiceJobsDataComponent>(station, out var queryJobData))
                return;
            UpdateUi((uid, console), (queryStation, queryJobData));
        }
    }

    private void UpdateUi(Entity<ServiceJobBoardConsoleComponent> ent, Entity<ServiceJobsDataComponent> stationEnt)
    {
        var state = new ServiceJobBoardConsoleState(
            GetJobs(stationEnt));

        _ui.SetUiState(ent.Owner, ServiceJobBoardUiKey.Key, state);
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
}
