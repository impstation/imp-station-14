using Content.Server._Impstation.GameTicking.Rules;
using Content.Server._Impstation.GameTicking.Rules.Components;
using Content.Server._Impstation.Slasher.Components;
using Content.Server.Audio;
using Content.Server.GameTicking;
using Content.Server.RoundEnd;
using Content.Shared._Impstation.Flash.Components;
using Content.Shared._Impstation.Slasher;
using Content.Shared._Impstation.Slasher.Components;
using Content.Shared.Audio;
using Content.Shared.GameTicking.Components;
using Robust.Server.Audio;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server._Impstation.Slasher;

/// <summary>
/// Handles the full victory sequence once the Slasher effigy is fully fed:
/// stinger, global flash effects, flash weakness application, mass teleport, boss spawn, and round end.
/// </summary>
public sealed class SlasherVictorySystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly SlasherDeathTeleportSystem _deathTeleport = default!;
    [Dependency] private readonly ContentAudioSystem _contentAudio = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    /// <summary>
    /// Subscribes local events and prepares dependencies for this system.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlasherRuleComponent, SlasherVictoryTimerFiredEvent>(OnVictoryTimerFired);
    }

    /// <summary>
    /// Runs the final slasher victory sequence once the delayed victory timer expires.
    /// </summary>
    /// <param name="rule">Rule entity and component data.</param>
    /// <param name="args">Victory timer event data.</param>
    private void OnVictoryTimerFired(Entity<SlasherRuleComponent> rule, ref SlasherVictoryTimerFiredEvent args)
    {
        // 1. Global stinger.
        _audio.PlayGlobal(new SoundPathSpecifier("/Audio/Items/Anomaly/shadow_crit.ogg"), Filter.Broadcast(), recordReplay: true, AudioParams.Default.WithVolume(-3f));

        // 2. Global screen-flash via the existing revenant-spook pulse event rule.
        _gameTicker.StartGameRule("SlasherRevenantSpookPulse");

        // 3. Everyone except Slashers gains flash weakness.
        ApplyCrewFlashWeakness();

        // 4. Teleport all living players into valid death-maze tiles.
        TeleportAllPlayersToDeathMaze();

        // 5. Spawn the final entity at death-maze origin.
        SpawnDeathMazeBoss();

        // 6. Override round-end lobby playlist for this victory.
        _contentAudio.TrySetNextRoundEndLobbyPlaylist(rule.Comp.VictoryLobbyMusicCollection);

        // 7. End the round directly (nuke-style) with a 5-minute restart countdown.
        _roundEnd.EndRound(rule.Comp.VictoryRoundEndDelay);
    }

    /// <summary>
    /// Applies flash weakness to all living players except Slashers,
    /// making them vulnerable to visual stuns during the victory sequence.
    /// </summary>
    private void ApplyCrewFlashWeakness()
    {
        var crewQuery = EntityQueryEnumerator<ActorComponent, TransformComponent>();
        while (crewQuery.MoveNext(out var uid, out _, out _))
        {
            if (HasComp<SlasherRoleComponent>(uid))
                continue;

            EnsureComp<FlashWeaknessComponent>(uid);
        }
    }

    /// <summary>
    /// Teleports all living players (including Slashers) to valid death-maze spawn locations.
    /// Uses separated spawn selection to distribute players throughout the maze.
    /// </summary>
    private void TeleportAllPlayersToDeathMaze()
    {
        var players = new List<EntityUid>();
        var query = EntityQueryEnumerator<ActorComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out _))
        {
            players.Add(uid);
        }

        var spawns = _deathTeleport.GetSeparatedDeathMazeSpawns(
            players.Count,
            DeathMazeSpawnSelection.Any);

        for (var i = 0; i < players.Count; i++)
        {
            var uid = players[i];
            if (i < spawns.Count)
            {
                if (spawns[i].IsValid(EntityManager))
                    _xform.SetCoordinates(uid, spawns[i]);

                continue;
            }

            if (_deathTeleport.TryGetDeathMazeSpawn(out var fallback, DeathMazeSpawnSelection.Any)
                && fallback.IsValid(EntityManager))
            {
                _xform.SetCoordinates(uid, fallback);
            }
        }
    }

    /// <summary>
    /// Spawns the final Slasher victory entity at the death-maze origin (0,0)
    /// and removes all boss spawner markers from the maze.
    /// </summary>
    private void SpawnDeathMazeBoss()
    {
        if (!_deathTeleport.TryGetDeathMazeOriginCoordinates(out var origin) || !origin.IsValid(EntityManager))
            return;

        Spawn("SlasherFinalEntity", origin);

        var spawnerQuery = EntityQueryEnumerator<SlasherBossSpawnerComponent, TransformComponent>();
        while (spawnerQuery.MoveNext(out var spawner, out _, out var spawnerXform))
        {
            if (spawnerXform.GridUid != _deathTeleport.DeathMazeGrid)
                continue;

            QueueDel(spawner);
        }
    }
}
