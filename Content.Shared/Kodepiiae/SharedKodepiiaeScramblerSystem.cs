using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Kodepiiae.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Kodepiiae;

public abstract partial class SharedKodepiiaeScramblerSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public sealed partial class KodepiiaeScramblerEvent : InstantActionEvent;

    [Serializable, NetSerializable]
    public sealed partial class KodepiiaeScramblerDoAfterEvent : SimpleDoAfterEvent;

    public void OnStartup(EntityUid uid, KodepiiaeScramblerComponent component, ComponentStartup args)
    {
        _actionsSystem.AddAction(uid, ref component.ScramblerAction, component.ScramblerActionId);
    }

    public void OnShutdown(EntityUid uid, KodepiiaeScramblerComponent component, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, component.ScramblerAction);
    }

    public void PlaySound(EntityUid uid, KodepiiaeScramblerComponent comp)
    {
        _audio.PlayPredicted(comp.ScramblerSound, uid, uid);
    }
}

