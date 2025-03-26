using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared._Impstation.Kodepiia.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Kodepiia;

public abstract partial class SharedKodepiiaScramblerSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public sealed partial class KodepiiaScramblerEvent : InstantActionEvent;

    [Serializable, NetSerializable]
    public sealed partial class KodepiiaScramblerDoAfterEvent : SimpleDoAfterEvent;

    public void OnStartup(EntityUid uid, KodepiiaScramblerComponent component, ComponentStartup args)
    {
        _actionsSystem.AddAction(uid, ref component.ScramblerAction, component.ScramblerActionId);
    }

    public void OnShutdown(EntityUid uid, KodepiiaScramblerComponent component, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, component.ScramblerAction);
    }

    public void PlaySound(EntityUid uid, KodepiiaScramblerComponent comp)
    {
        _audio.PlayPredicted(comp.ScramblerSound, uid, uid);
    }
}

