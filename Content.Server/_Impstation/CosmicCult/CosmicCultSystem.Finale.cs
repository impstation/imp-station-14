
using Content.Server._Impstation.CosmicCult.Components;
using Content.Shared._Impstation.CosmicCult;
using Content.Shared._Impstation.CosmicCult.Components;
using Content.Shared.Audio;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Robust.Shared.Audio;
using Robust.Shared.Utility;

namespace Content.Server._Impstation.CosmicCult;

public sealed partial class CosmicCultSystem : EntitySystem
{

    /// <summary>
    ///     Used to calculate when the finale song should start playing
    /// </summary>
    private TimeSpan _finaleSongLength;
    private string _selectedFinaleSong = String.Empty;
    private string _selectedBufferSong = String.Empty;
    private TimeSpan _interactionTime = TimeSpan.FromSeconds(2);
    private TimeSpan _restartCoolDown = TimeSpan.FromSeconds(60);
    private SoundSpecifier _bufferMusic = new SoundPathSpecifier("/Audio/_Impstation/CosmicCult/finale_buffertimer_placeholder.ogg");
    private SoundSpecifier _finaleMusic = new SoundPathSpecifier("/Audio/_Impstation/CosmicCult/finale_finaletimer_placeholder.ogg");
    public void SubscribeFinale()
    {

        SubscribeLocalEvent<CosmicFinaleComponent, InteractHandEvent>(OnInteract);
        SubscribeLocalEvent<CosmicFinaleComponent, StartFinaleDoAfterEvent>(OnFinaleStartDoAfter);
        SubscribeLocalEvent<CosmicFinaleComponent, CancelFinaleDoAfterEvent>(OnFinaleCancelDoAfter);
    }

    private void OnInteract(Entity<CosmicFinaleComponent> uid, ref InteractHandEvent args)
    {
        if (!HasComp<CosmicCultComponent>(args.User) && uid.Comp.FinaleActive && !args.Handled)
        {
            uid.Comp.Occupied = true;
            var doargs = new DoAfterArgs(EntityManager, args.User, _interactionTime, new CancelFinaleDoAfterEvent(), uid, uid)
            {
                DistanceThreshold = 1f, Hidden = false, BreakOnHandChange = true, BreakOnDamage = true, BreakOnMove = true
            };
            _popup.PopupEntity(Loc.GetString("cosmiccult-finale-cancel-begin"), args.User, args.User);
            _doAfter.TryStartDoAfter(doargs);
        }
        else if (HasComp<CosmicCultComponent>(args.User) && uid.Comp.FinaleReady && !args.Handled)
        {
            uid.Comp.Occupied = true;
            var doargs = new DoAfterArgs(EntityManager, args.User, _interactionTime, new StartFinaleDoAfterEvent(), uid, uid)
            {
                DistanceThreshold = 1f, Hidden = false, BreakOnHandChange = true, BreakOnDamage = true, BreakOnMove = true
            };
            _popup.PopupEntity(Loc.GetString("cosmiccult-finale-beckon-begin"), args.User, args.User);
            _doAfter.TryStartDoAfter(doargs);
        }
        else return;
        args.Handled = true;
    }

    private void OnFinaleStartDoAfter(Entity<CosmicFinaleComponent> uid, ref StartFinaleDoAfterEvent args)
    {
        var comp = uid.Comp;
        if (args.Args.Target == null || args.Cancelled || args.Handled || !TryComp<MonumentComponent>(args.Args.Target, out var monument))
        {
            uid.Comp.Occupied = false;
            return;
        }
        _popup.PopupEntity(Loc.GetString("cosmiccult-finale-beckon-success"), args.Args.User, args.Args.User);
        if (!comp.BufferComplete)
        {
            comp.BufferTimer = _timing.CurTime + comp.BufferRemainingTime;
            _selectedBufferSong = _audio.GetSound(_bufferMusic);
            _sound.DispatchStationEventMusic(uid, _selectedBufferSong, StationEventMusicType.Nuke);
        }
        else
        {
            comp.FinaleTimer = _timing.CurTime + comp.FinaleRemainingTime;
            _selectedFinaleSong = _audio.GetSound(_finaleMusic);
            _finaleSongLength = TimeSpan.FromSeconds(_audio.GetAudioLength(_selectedFinaleSong).TotalSeconds);
            _sound.DispatchStationEventMusic(uid, _selectedFinaleSong, StationEventMusicType.Nuke);
        }
        comp.FinaleReady = false;
        comp.FinaleActive = true;
        monument.Enabled = true;
    }
    private void OnFinaleCancelDoAfter(Entity<CosmicFinaleComponent> uid, ref CancelFinaleDoAfterEvent args)
    {
        var comp = uid.Comp;
        if (args.Args.Target == null || args.Cancelled || args.Handled)
        {
            uid.Comp.Occupied = false;
            return;
        }
        var ev = new FinaleCancelledEvent();
        RaiseLocalEvent(ev);
        _sound.PlayGlobalOnStation(uid, _audio.GetSound(comp.CancelEventSound));
        _sound.StopStationEventMusic(uid, StationEventMusicType.Nuke);
        if (!comp.BufferComplete) comp.BufferRemainingTime = comp.BufferTimer - _timing.CurTime + TimeSpan.FromSeconds(15);
        else comp.FinaleRemainingTime = comp.FinaleTimer - _timing.CurTime;
        comp.PlayedFinaleSong = false;
        comp.PlayedBufferSong = false;
        comp.FinaleActive = false;
        comp.FinaleReady = true;
        Log.Debug($"{comp.FinaleRemainingTime} time remaining in Finale.");
        Log.Debug($"{comp.BufferRemainingTime} time remaining in Buffer.");
    }
}
public sealed class FinaleCancelledEvent : EntityEventArgs
{

}
