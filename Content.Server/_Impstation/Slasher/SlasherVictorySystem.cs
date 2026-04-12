using Content.Server._Impstation.GameTicking.Rules;
using Content.Server._Impstation.GameTicking.Rules.Components;
using Content.Server._Impstation.Slasher.Components;
using Content.Server.GameTicking;
using Content.Server.RoundEnd;
using Content.Shared._Impstation.CombatModeSprint;
using Content.Shared._Impstation.Slasher;
using Content.Shared._Impstation.Slasher.Components;
using Content.Shared.Audio;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.Movement.Systems;
using Robust.Server.Audio;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server._Impstation.Slasher;

/// <summary>
/// Handles the full victory sequence once the Slasher effigy is fully fed:
/// stinger, global flash, crew teleport, Slasher teleport, portal deletion, boss spawn, shuttle call.
/// Also bumps the Slasher's combat sprint coefficient for the death-maze phase.
/// </summary>
public sealed class SlasherVictorySystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly SlasherDeathTeleportSystem _deathTeleport = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
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
    /// Type definition for OnVictoryTimerFired.
    /// </summary>
    /// <param name="rule">Parameter used by this method: rule.</param>
    /// <param name="args">Event arguments for this callback.</param>
    private void OnVictoryTimerFired(Entity<SlasherRuleComponent> rule, ref SlasherVictoryTimerFiredEvent args)
    {
        // 1. Global stinger.
        _audio.PlayGlobal(new SoundPathSpecifier("/Audio/Items/Anomaly/shadow_crit.ogg"), Filter.Broadcast(), recordReplay: true, AudioParams.Default.WithVolume(-3f));

        // 2. Global screen-flash via the existing revenant-spook pulse event rule.
        _gameTicker.StartGameRule("SlasherRevenantSpookPulse");

        TeleportCrewToDeathMaze();
        var (portalCoords, portalEnts) = CollectDeathMazePortals();
        TeleportSlashersToPortalAndBuff(rule.Comp.DeathMazeSprintCoefficient, portalCoords);
        DeletePortals(portalEnts);
        SpawnDeathMazeBosses();

        // 8. End the round directly (nuke-style) with a 5-minute restart countdown.
        _roundEnd.EndRound(rule.Comp.VictoryRoundEndDelay);
    }

    private void TeleportCrewToDeathMaze()
    {
        var crewQuery = EntityQueryEnumerator<ActorComponent, TransformComponent>();
        while (crewQuery.MoveNext(out var uid, out _, out _))
        {
            if (HasComp<SlasherRoleComponent>(uid))
                continue;

            if (_deathTeleport.TryGetDeathMazeSpawn(out var crewDest))
                _xform.SetCoordinates(uid, crewDest);
        }
    }

    private (List<EntityCoordinates> PortalCoords, List<EntityUid> PortalEnts) CollectDeathMazePortals()
    {
        var portalCoords = new List<EntityCoordinates>();
        var portalEnts = new List<EntityUid>();

        var riftQuery = EntityQueryEnumerator<SlasherRiftTeleportComponent, TransformComponent>();
        while (riftQuery.MoveNext(out var riftUid, out _, out var riftXform))
        {
            if (riftXform.GridUid != _deathTeleport.DeathMazeGrid)
                continue;

            portalCoords.Add(riftXform.Coordinates);
            portalEnts.Add(riftUid);
        }

        return (portalCoords, portalEnts);
    }

    private void TeleportSlashersToPortalAndBuff(float sprintCoefficient, List<EntityCoordinates> portalCoords)
    {
        var destination = portalCoords.Count > 0 ? portalCoords[0] : EntityCoordinates.Invalid;
        var slasherQuery = EntityQueryEnumerator<SlasherRoleComponent, TransformComponent>();
        while (slasherQuery.MoveNext(out var slasher, out _, out _))
        {
            if (destination.IsValid(EntityManager))
                _xform.SetCoordinates(slasher, destination);

            if (!TryComp<CombatModeSprintComponent>(slasher, out var sprint))
                continue;

            sprint.SprintCoefficient = sprintCoefficient;
            Dirty(slasher, sprint);
            _movementSpeed.RefreshMovementSpeedModifiers(slasher);
        }
    }

    private void DeletePortals(List<EntityUid> portalEnts)
    {
        foreach (var portal in portalEnts)
            QueueDel(portal);
    }

    private void SpawnDeathMazeBosses()
    {
        var spawnerQuery = EntityQueryEnumerator<SlasherBossSpawnerComponent, TransformComponent>();
        while (spawnerQuery.MoveNext(out var spawner, out _, out var spawnerXform))
        {
            if (spawnerXform.GridUid != _deathTeleport.DeathMazeGrid)
                continue;

            Spawn("SlasherFinalEntity", spawnerXform.Coordinates);
            QueueDel(spawner);
        }
    }
}
