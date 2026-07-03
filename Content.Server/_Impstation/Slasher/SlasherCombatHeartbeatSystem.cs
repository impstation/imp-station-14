using Content.Server._Impstation.Slasher.Components;
using Content.Shared.CombatMode;
using Robust.Server.Audio;
using Robust.Shared.Timing;

namespace Content.Server._Impstation.Slasher;

/// <summary>
/// Emits heartbeat audio around Slashers while they are in combat mode.
/// </summary>
public sealed class SlasherCombatHeartbeatSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <summary>
    /// Subscribes combat-mode change handling for heartbeat timing initialization.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlasherCombatHeartbeatComponent, CombatModeChangedEvent>(OnCombatModeChanged);
    }

    /// <summary>
    /// Primes the heartbeat timer when combat mode toggles on so the first beat fires immediately.
    /// </summary>
    /// <param name="ent">Entity and heartbeat component data.</param>
    /// <param name="args">Combat mode change event data.</param>
    private void OnCombatModeChanged(Entity<SlasherCombatHeartbeatComponent> ent, ref CombatModeChangedEvent args)
    {
        if (args.Enabled)
            ent.Comp.NextBeatTime = _timing.CurTime;
    }

    /// <summary>
    /// Emits heartbeat audio for Slashers currently in combat mode at the configured beat interval.
    /// </summary>
    /// <param name="frameTime">Frame delta time in seconds.</param>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<SlasherCombatHeartbeatComponent, CombatModeComponent>();
        while (query.MoveNext(out var uid, out var heartbeat, out var combatMode))
        {
            if (!combatMode.IsInCombatMode || now < heartbeat.NextBeatTime)
                continue;

            _audio.PlayPvs(heartbeat.HeartbeatSound, uid);
            heartbeat.NextBeatTime = now + heartbeat.BeatInterval;
        }
    }
}
