using Content.Shared._Impstation.Kodepiiae.Components;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
<<<<<<<< HEAD:Content.Shared/_Impstation/Kodepiia/SharedKodepiiaScramblerSystem.cs
using Content.Shared.Humanoid;
using Content.Shared._Impstation.Kodepiia.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Kodepiia;
========
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Kodepiiae;
>>>>>>>> f763918bc7 (Finish up consuming and cleaned up code):Content.Shared/_Impstation/Kodepiiae/SharedKodepiiaeScramblerSystem.cs

public abstract partial class SharedKodepiiaScramblerSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public sealed partial class KodepiiaScramblerEvent : InstantActionEvent;

    [Serializable, NetSerializable]
    public sealed partial class KodepiiaScramblerDoAfterEvent : SimpleDoAfterEvent;

<<<<<<<< HEAD:Content.Shared/_Impstation/Kodepiia/SharedKodepiiaScramblerSystem.cs
    public void OnStartup(EntityUid uid, KodepiiaScramblerComponent component, ComponentStartup args)
========
    public void OnStartup(Entity<KodepiiaeScramblerComponent> ent, ref ComponentStartup args)
>>>>>>>> f763918bc7 (Finish up consuming and cleaned up code):Content.Shared/_Impstation/Kodepiiae/SharedKodepiiaeScramblerSystem.cs
    {
        _actionsSystem.AddAction(ent, ref ent.Comp.ScramblerAction, ent.Comp.ScramblerActionId);
    }

<<<<<<<< HEAD:Content.Shared/_Impstation/Kodepiia/SharedKodepiiaScramblerSystem.cs
    public void OnShutdown(EntityUid uid, KodepiiaScramblerComponent component, ComponentShutdown args)
========
    public void OnShutdown(Entity<KodepiiaeScramblerComponent> ent, ref ComponentShutdown args)
>>>>>>>> f763918bc7 (Finish up consuming and cleaned up code):Content.Shared/_Impstation/Kodepiiae/SharedKodepiiaeScramblerSystem.cs
    {
        _actionsSystem.RemoveAction(ent, ent.Comp.ScramblerAction);
    }

<<<<<<<< HEAD:Content.Shared/_Impstation/Kodepiia/SharedKodepiiaScramblerSystem.cs
    public void PlaySound(EntityUid uid, KodepiiaScramblerComponent comp)
========
    public void PlaySound(EntityUid uid, Components.KodepiiaeScramblerComponent comp)
>>>>>>>> f763918bc7 (Finish up consuming and cleaned up code):Content.Shared/_Impstation/Kodepiiae/SharedKodepiiaeScramblerSystem.cs
    {
        _audio.PlayPredicted(comp.ScramblerSound, uid, uid);
    }
}

