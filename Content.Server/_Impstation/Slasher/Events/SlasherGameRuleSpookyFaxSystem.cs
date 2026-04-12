using Content.Server.Fax;
using Content.Server.GameTicking.Rules;
using Content.Shared.Fax.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;

namespace Content.Server._Impstation.Slasher.Events;

/// <summary>
/// Sends creepy fax printouts to command and randomized additional station fax machines.
/// </summary>
public sealed class SlasherGameRuleSpookyFaxSystem : SlasherPulseGameRuleSystem<SlasherGameRuleSpookyFaxComponent>
{
    [Dependency] private readonly FaxSystem _fax = default!;

    /// <summary>
    /// Selects target fax machines on a random station and delivers one randomized spooky printout.
    /// </summary>
    /// <param name="uid">Rule entity UID.</param>
    /// <param name="component">Rule configuration component.</param>
    /// <param name="gameRule">Base game-rule component.</param>
    /// <param name="args">Rule start event data.</param>
    protected override void Started(EntityUid uid, SlasherGameRuleSpookyFaxComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (component.MessageLocaleKeys.Count == 0)
            return;

        if (!TryGetPulseStation(out var chosenStation))
            return;

        var stationFaxes = new List<EntityUid>();
        var faxQuery = EntityQueryEnumerator<FaxMachineComponent, TransformComponent>();
        while (faxQuery.MoveNext(out var faxUid, out _, out var xform))
        {
            if (CompOrNull<StationMemberComponent>(xform.GridUid)?.Station != chosenStation)
                continue;

            stationFaxes.Add(faxUid);
        }

        if (stationFaxes.Count == 0)
            return;

        var messageKey = component.MessageLocaleKeys[RobustRandom.Next(component.MessageLocaleKeys.Count)];
        var messageBody = Loc.GetString(messageKey);
        var printout = new FaxPrintout(messageBody, component.PrintoutTitle);

        var selected = new HashSet<EntityUid>();

        foreach (var faxUid in stationFaxes)
        {
            if (!TryComp<FaxMachineComponent>(faxUid, out var faxComp) || !faxComp.ReceiveNukeCodes)
                continue;

            selected.Add(faxUid);
            break;
        }

        var randomCandidates = new List<EntityUid>();
        foreach (var faxUid in stationFaxes)
        {
            if (selected.Contains(faxUid))
                continue;

            randomCandidates.Add(faxUid);
        }

        RobustRandom.Shuffle(randomCandidates);

        var randomCount = RobustRandom.Next(component.MinRandomFaxes, component.MaxRandomFaxes + 1);
        randomCount = Math.Clamp(randomCount, 0, randomCandidates.Count);

        for (var i = 0; i < randomCount; i++)
            selected.Add(randomCandidates[i]);

        foreach (var faxUid in selected)
        {
            if (!TryComp<FaxMachineComponent>(faxUid, out var faxComp))
                continue;

            _fax.Receive(faxUid, printout, component: faxComp);
        }
    }
}
