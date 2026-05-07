using Content.Server.GameTicking.Rules;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Medical.CrewMonitoring;
using Content.Shared._Impstation.Slasher.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Medical.SuitSensor;

namespace Content.Server._Impstation.Slasher.Events;

/// <summary>
/// Injects temporary fake crew-monitor statuses into active monitoring servers as a Slasher pulse effect.
/// </summary>
public sealed class SlasherGameRuleMassCasualitySystem : GameRuleSystem<SlasherGameRuleMassCasualityComponent>
{
    private readonly List<PhantomInjection> _activeInjections = new();

    [Dependency] private readonly CrewMonitoringServerSystem _crewMonitor = default!;
    [Dependency] private readonly SingletonDeviceNetServerSystem _singletonServers = default!;

    /// <summary>
    /// Maintains active phantom entries by re-sending packets until their expiration time.
    /// </summary>
    /// <param name="frameTime">Frame delta in seconds.</param>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = Timing.CurTime;
        for (var i = _activeInjections.Count - 1; i >= 0; i--)
        {
            var entry = _activeInjections[i];
            if (!Exists(entry.ServerUid) || now >= entry.ExpiresAt)
            {
                _crewMonitor.RemoveSensorStatusByAddress(entry.ServerUid, entry.Address);

                _activeInjections.RemoveAt(i);
                continue;
            }

            if (now < entry.NextSendAt)
                continue;

            InjectPhantom(entry.ServerUid, entry.Address, entry.Status);
            entry.NextSendAt = now + TimeSpan.FromSeconds(2);
            _activeInjections[i] = entry;
        }
    }

    /// <summary>
    /// Seeds phantom casualties using current server snapshots and weighted status rolls.
    /// </summary>
    /// <param name="uid">Rule entity UID.</param>
    /// <param name="component">Rule configuration component.</param>
    /// <param name="gameRule">Base game-rule component.</param>
    /// <param name="args">Rule start event data.</param>
    protected override void Started(EntityUid uid, SlasherGameRuleMassCasualityComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var servers = GetActiveServers();
        if (servers.Count == 0)
            return;

        var templates = GetAggregatedSensorSnapshot(servers);
        templates.RemoveAll(t =>
        {
            var owner = GetEntity(t.OwnerUid);
            return !Exists(owner) || HasComp<SlasherRoleComponent>(owner);
        });

        if (templates.Count == 0)
            return;

        var count = RobustRandom.Next(component.MinEntries, component.MaxEntries + 1);
        var duration = TimeSpan.FromSeconds(component.DurationSeconds);

        for (var i = 0; i < count; i++)
        {
            var template = templates[RobustRandom.Next(templates.Count)];
            var fake = new SuitSensorStatus(template.OwnerUid, template.SuitSensorUid, template.Name, template.Job, template.JobIcon, template.JobDepartments)
            {
                Coordinates = template.Coordinates,
                Timestamp = Timing.CurTime,
            };

            var statusRoll = RollStatus(component.DeadWeight, component.CritWeight, component.BadWeight);
            var threshold = template.TotalDamageThreshold ?? 200;

            switch (statusRoll)
            {
                case PhantomStatus.Dead:
                    fake.IsAlive = false;
                    fake.TotalDamage = threshold + 50;
                    fake.TotalDamageThreshold = threshold;
                    break;
                case PhantomStatus.Critical:
                    fake.IsAlive = true;
                    // Crew monitor UI uses rounded damage buckets and only shows the critical icon
                    // at the highest bucket, so ensure this value crosses that threshold.
                    fake.TotalDamage = threshold + Math.Max(25, threshold / 4);
                    fake.TotalDamageThreshold = threshold;
                    break;
                default:
                    fake.IsAlive = true;
                    fake.TotalDamage = Math.Max(1, (int)(threshold * 0.8f));
                    fake.TotalDamageThreshold = threshold;
                    break;
            }

            foreach (var server in servers)
            {
                var address = $"slasher-phantom-{uid}-{i}-{RobustRandom.Next(1000000)}";
                InjectPhantom(server, address, fake);
                _activeInjections.Add(new PhantomInjection(
                    server,
                    address,
                    fake,
                    Timing.CurTime + duration,
                    Timing.CurTime + TimeSpan.FromSeconds(2)));
            }
        }
    }

    /// <summary>
    /// Collects active crew-monitoring singleton servers.
    /// </summary>
    /// <returns>List of active server entity UIDs.</returns>
    private List<EntityUid> GetActiveServers()
    {
        var servers = new List<EntityUid>();
        var inactiveServers = new List<EntityUid>();
        var query = EntityQueryEnumerator<CrewMonitoringServerComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (_singletonServers.IsActiveServer(uid))
                servers.Add(uid);
            else
                inactiveServers.Add(uid);
        }

        // Fallback so pulses can still apply when no monitor server has been activated yet.
        if (servers.Count == 0)
            return inactiveServers;

        return servers;
    }

    /// <summary>
    /// Builds a de-duplicated snapshot of suit-sensor statuses aggregated across all servers.
    /// </summary>
    /// <param name="serverUids">Servers to sample.</param>
    /// <returns>Unique sensor status snapshots.</returns>
    private List<SuitSensorStatus> GetAggregatedSensorSnapshot(List<EntityUid> serverUids)
    {
        var snapshots = new List<SuitSensorStatus>();
        var seen = new HashSet<string>();

        foreach (var serverUid in serverUids)
        {
            if (!TryComp<CrewMonitoringServerComponent>(serverUid, out var component))
                continue;

            foreach (var status in component.SensorStatus.Values)
            {
                var key = $"{status.OwnerUid}:{status.SuitSensorUid}";
                if (!seen.Add(key))
                    continue;

                snapshots.Add(status);
            }
        }

        return snapshots;
    }

    /// <summary>
    /// Sends a single phantom status packet to a crew-monitoring server address.
    /// </summary>
    /// <param name="serverUid">Destination server entity UID.</param>
    /// <param name="address">Device network address used for the phantom entry.</param>
    /// <param name="status">Status payload to transmit.</param>
    private void InjectPhantom(EntityUid serverUid, string address, SuitSensorStatus status)
    {
        status.Timestamp = Timing.CurTime;
        _crewMonitor.SetSensorStatusByAddress(serverUid, address, status);
    }

    /// <summary>
    /// Rolls one phantom casualty state from configured weighted probabilities.
    /// </summary>
    /// <param name="deadWeight">Weight for dead states.</param>
    /// <param name="critWeight">Weight for critical states.</param>
    /// <param name="badWeight">Weight for heavily injured-but-alive states.</param>
    /// <returns>Rolled phantom status value.</returns>
    private PhantomStatus RollStatus(int deadWeight, int critWeight, int badWeight)
    {
        var dead = Math.Max(0, deadWeight);
        var crit = Math.Max(0, critWeight);
        var bad = Math.Max(0, badWeight);

        var total = dead + crit + bad;
        if (total <= 0)
            return PhantomStatus.Bad;

        var roll = RobustRandom.Next(total);
        if (roll < dead)
            return PhantomStatus.Dead;

        roll -= dead;
        if (roll < crit)
            return PhantomStatus.Critical;

        return PhantomStatus.Bad;
    }

    /// <summary>
    /// Enumeration values for PhantomStatus.
    /// </summary>
    private enum PhantomStatus
    {
        Bad,
        Critical,
        Dead,
    }

    /// <summary>
    /// Event payload record for struct.
    /// </summary>
    private record struct PhantomInjection(
        EntityUid ServerUid,
        string Address,
        SuitSensorStatus Status,
        TimeSpan ExpiresAt,
        TimeSpan NextSendAt);
}
