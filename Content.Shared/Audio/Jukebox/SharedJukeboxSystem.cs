using Robust.Shared.Audio.Systems;
using Content.Shared.Emag.Systems; //imp
using Robust.Shared.Serialization; // Frontier

namespace Content.Shared.Audio.Jukebox;

public abstract class SharedJukeboxSystem : EntitySystem
{
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] private readonly EmagSystem _emag = default!; // imp

    //imp start - support emagging the jukebox
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<JukeboxComponent, GotEmaggedEvent>(OnEmagged);
    }

    protected void OnEmagged(EntityUid uid, JukeboxComponent component, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(uid, EmagType.Interaction))
            return;

        args.Handled = true;
    }

    /// <summary>
    /// Returns whether or not the given jukebox is currently playing a song.
    /// </summary>
    public bool IsPlaying(Entity<JukeboxComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        return entity.Comp.AudioStream is { } audio && Audio.IsPlaying(audio);
    }
}

// Frontier: Shuffle & Repeat
[Serializable, NetSerializable]
public sealed class JukeboxInterfaceState(JukeboxPlaybackMode playbackMode) : BoundUserInterfaceState
{
    public JukeboxPlaybackMode PlaybackMode { get; set; } = playbackMode;
}
// End Frontier: Shuffle & Repeat
