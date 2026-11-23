using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Audio.Jukebox;
using Content.Shared.Power;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random; // Frontier

namespace Content.Server.Audio.Jukebox;

public sealed class JukeboxSystem : SharedJukeboxSystem
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!; // Frontier
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!; // Frontier

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<JukeboxComponent, JukeboxSelectedMessage>(OnJukeboxSelected);
        SubscribeLocalEvent<JukeboxComponent, JukeboxPlayingMessage>(OnJukeboxPlay);
        SubscribeLocalEvent<JukeboxComponent, JukeboxPauseMessage>(OnJukeboxPause);
        SubscribeLocalEvent<JukeboxComponent, JukeboxStopMessage>(OnJukeboxStop);
        SubscribeLocalEvent<JukeboxComponent, JukeboxSetTimeMessage>(OnJukeboxSetTime);
        SubscribeLocalEvent<JukeboxComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<JukeboxComponent, ComponentShutdown>(OnComponentShutdown);

        SubscribeLocalEvent<JukeboxComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<JukeboxComponent, JukeboxSetPlaybackModeMessage>(OnJukeboxSetPlayback); // Frontier
        SubscribeLocalEvent<JukeboxComponent, ComponentStartup>(OnComponentStartup); // Frontier
    }

    // Frontier: Shuffle & Repeat
    private void OnComponentStartup(Entity<JukeboxComponent> entity, ref ComponentStartup ev)
    {
        UpdateUI(entity);
    }

    private void UpdateUI(Entity<JukeboxComponent> ent)
    {
        var state = new JukeboxInterfaceState(ent.Comp.PlaybackMode);
        _userInterface.SetUiState(ent.Owner, JukeboxUiKey.Key, state);
    }

    private void OnJukeboxSetPlayback(Entity<JukeboxComponent> ent, ref JukeboxSetPlaybackModeMessage playbackModeMessage)
    {
        if (ent.Comp.PlaybackMode != playbackModeMessage.PlaybackMode)
        {
            ent.Comp.PlaybackMode = playbackModeMessage.PlaybackMode;
            UpdateUI(ent);
            Dirty(ent);
        }
    }

    public AudioState GetAudioState(EntityUid? entity, AudioComponent? component = null)
    {
        if (entity == null || !Resolve(entity.Value, ref component, false))
            return AudioState.Stopped; // Consider no audio as stopped.

        return component.State;
    }
    // End Frontier: Shuffle & Repeat


    private void OnComponentInit(Entity<JukeboxComponent> ent, ref ComponentInit args)
    {
        if (HasComp<ApcPowerReceiverComponent>(ent))
        {
            TryUpdateVisualState(ent.AsNullable());
        }
    }

    private void OnJukeboxPlay(Entity<JukeboxComponent> ent, ref JukeboxPlayingMessage args)
    {
        TryPlay(ent.AsNullable());
    }

    private void OnJukeboxPause(Entity<JukeboxComponent> ent, ref JukeboxPauseMessage args)
    {
        Pause(ent.AsNullable());
    }

    private void OnJukeboxSetTime(Entity<JukeboxComponent> ent, ref JukeboxSetTimeMessage args)
    {
        if (TryComp(args.Actor, out ActorComponent? actorComp))
        {
            var offset = actorComp.PlayerSession.Channel.Ping * 1.5f / 1000f;
            SetTime(ent.AsNullable(), args.SongTime + offset);
        }
    }

    private void OnPowerChanged(Entity<JukeboxComponent> entity, ref PowerChangedEvent args)
    {
        TryUpdateVisualState(entity.AsNullable());

        if (!this.IsPowered(entity.Owner, EntityManager))
        {
            Stop(entity.AsNullable());
        }
    }

    private void OnJukeboxStop(Entity<JukeboxComponent> entity, ref JukeboxStopMessage args)
    {
        Stop(entity.AsNullable());
    }

    private void OnJukeboxSelected(EntityUid uid, JukeboxComponent component, JukeboxSelectedMessage args)
    {
        SetSelectedTrack((uid, component), args.SongId);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<JukeboxComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Selecting)
            {
                comp.SelectAccumulator += frameTime;
                if (comp.SelectAccumulator >= 0.5f)
                {
                    comp.SelectAccumulator = 0f;
                    comp.Selecting = false;

                    TryUpdateVisualState((uid, comp));
                }
            }

            // Frontier: Replay feature. Please pitch in if you have better ideas. This is a pretty bad implementation.
            if (comp.PlaybackMode != JukeboxPlaybackMode.Single && comp.AudioStream != null &&
                GetAudioState(comp.AudioStream) == AudioState.Stopped)
            {
                TryPlay(uid);
            }
            // End Frontier
        }
    }

    private void OnComponentShutdown(Entity<JukeboxComponent> ent, ref ComponentShutdown args)
    {
        ent.Comp.AudioStream = Audio.Stop(ent.Comp.AudioStream);
    }

    private void DirectSetVisualState(EntityUid uid, JukeboxVisualState state)
    {
        _appearanceSystem.SetData(uid, JukeboxVisuals.VisualState, state);
    }

    private void TryUpdateVisualState(Entity<JukeboxComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var finalState = JukeboxVisualState.On;

        if (!this.IsPowered(ent, EntityManager))
        {
            finalState = JukeboxVisualState.Off;
        }

        _appearanceSystem.SetData(ent, JukeboxVisuals.VisualState, finalState);
    }

    /// <summary>
    /// Set the selected track of the jukebox to the specified prototype.
    /// </summary>
    public void SetSelectedTrack(Entity<JukeboxComponent?> ent, ProtoId<JukeboxPrototype> track)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        // Frontier: allow selecting songs while they're playing
        var wasPlaying = Audio.IsPlaying(ent.Comp.AudioStream);

        // if (!Audio.IsPlaying(ent.Comp.AudioStream))
        // {
        // ok look i know these indentations look gross but PLEEEASE keep them
            ent.Comp.SelectedSongId = track;
            DirectSetVisualState(ent, JukeboxVisualState.Select);
            ent.Comp.Selecting = true;
        ent.Comp.SelectAccumulator = 0;
            ent.Comp.AudioStream = Audio.Stop(ent.Comp.AudioStream);
        ent.Comp.FirstPlay = true; // Prevent shuffling

        if (wasPlaying)
        {
            TryPlay(ent);
        }

        Dirty(ent);
        // }
        // End Frontier
    }

    /// <summary>
    /// Attempts to play the jukebox's current selected track.
    /// </summary>
    /// <returns>false if no track is selected or the track prototype cannot be found, otherwise true.</returns>
    public bool TryPlay(Entity<JukeboxComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (Exists(ent.Comp.AudioStream))
        {
            Audio.SetState(ent.Comp.AudioStream, AudioState.Playing);
        }
        else
        {
            // Frontier: Shuffling feature.
            if (ent.Comp.PlaybackMode == JukeboxPlaybackMode.Shuffle
                && !ent.Comp.FirstPlay
                && _protoManager.TryGetRandom<JukeboxPrototype>(_random, out var newProto)
                && newProto is JukeboxPrototype newJukeboxProto)
            {
                ent.Comp.SelectedSongId = newJukeboxProto;
            }
            // End Frontier

            if (string.IsNullOrEmpty(ent.Comp.SelectedSongId) ||
                !_protoManager.Resolve(ent.Comp.SelectedSongId, out var jukeboxProto))
            {
                return false;
            }

            ent.Comp.FirstPlay = false; // Frontier: shuffle state

            ent.Comp.AudioStream = Audio.PlayPvs(jukeboxProto.Path, ent, AudioParams.Default.WithMaxDistance(10f))?.Entity;
            Dirty(ent);
        }
        return true;
    }

    /// <summary>
    /// Stops any track that may currently be playing.
    /// </summary>
    public void Stop(Entity<JukeboxComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, logMissing: false))
            return;

        // FRONTIER START
        //Audio.SetState(entity.Comp.AudioStream, AudioState.Stopped); // No longer needed since we're removing the AudioStream.
        entity.Comp.AudioStream = Audio.Stop(entity.Comp.AudioStream);
        entity.Comp.FirstPlay = true;
        // FRONTIER END
        Dirty(entity);
    }

    /// <summary>
    /// Pauses any track that may currently be playing.
    /// </summary>
    public void Pause(Entity<JukeboxComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, logMissing: false))
            return;

        Audio.SetState(entity.Comp.AudioStream, AudioState.Paused);
    }

    /// <summary>
    /// Sets the playback position within the current audio track.
    /// </summary>
    /// <remarks>
    /// If setting based on user input, you may need to compensate for the player's ping.
    /// </remarks>
    public void SetTime(Entity<JukeboxComponent?> entity, float songTime)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        Audio.SetPlaybackPosition(entity.Comp.AudioStream, songTime);
    }
}
