using Content.Server._Impstation.GameTicking.Rules.Components;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.GameTicking.Components;

namespace Content.Server.GameTicking.Rules;

public sealed partial class RoundstartStationVariationRuleSystem
{
    private void OnPlayersSpawned(RulePlayerJobsAssignedEvent ev)
    {
        // as long as one is running
        if (!GameTicker.IsGameRuleAdded<RoundstartStationVariationRuleComponent>())
            return;

        Log.Info($"Running variation rules for players");

        var players = new List<EntityUid>();

        foreach (var player in ev.Players)
        {
            if (player.AttachedEntity is not { } playerEntity)
            {
                Log.Error($"Player {player.Name} ({player.UserId}) has no attached entity, skipping for player variation pass");
                continue;
            }

            players.Add(playerEntity);
        }

        // raise the event on any passes that have been added
        var passEv = new PlayerVariationPassEvent(players);
        var passQuery = EntityQueryEnumerator<PlayerVariationPassRuleComponent, GameRuleComponent>();
        while (passQuery.MoveNext(out var uid, out _, out _))
            RaiseLocalEvent(uid, ref passEv);
    }
}

/// <summary>
///     Raised when player variation rules should be applied.
/// </summary>
/// <param name="Players">List of player entities.</param>
[ByRefEvent]
public readonly record struct PlayerVariationPassEvent(List<EntityUid> Players);
