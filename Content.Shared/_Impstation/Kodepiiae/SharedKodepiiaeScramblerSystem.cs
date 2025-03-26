using Content.Shared._Impstation.Kodepiiae.Components;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Kodepiiae;

public abstract partial class SharedKodepiiaeScramblerSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public sealed partial class KodepiiaeScramblerEvent : InstantActionEvent;

    [Serializable, NetSerializable]
    public sealed partial class KodepiiaeScramblerDoAfterEvent : SimpleDoAfterEvent;

    public void OnStartup(Entity<KodepiiaeScramblerComponent> ent, ref ComponentStartup args)
    {
        _actionsSystem.AddAction(ent, ref ent.Comp.ScramblerAction, ent.Comp.ScramblerActionId);
    }

    public void OnShutdown(Entity<KodepiiaeScramblerComponent> ent, ref ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(ent, ent.Comp.ScramblerAction);
    }

    public void PlaySound(EntityUid uid, Components.KodepiiaeScramblerComponent comp)
    {
        _audio.PlayPredicted(comp.ScramblerSound, uid, uid);
    }
}

