using Content.Shared._Impstation.Kodepiiae.Components;
using Content.Shared.Actions;
using Content.Shared.Body.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Kodepiiae;

public abstract partial class SharedKodepiiaeConsumeSystem : EntitySystem
{
    public sealed partial class KodepiiaeConsumeEvent : EntityTargetActionEvent;

    [Serializable, NetSerializable]
    public sealed partial class KodepiiaeConsumeDoAfterEvent : SimpleDoAfterEvent;

    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    public void OnShutdown(Entity<KodepiiaeConsumeActionComponent> ent, ref ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(ent, ent.Comp.ConsumeAction);
    }
    public void OnStartup(Entity<KodepiiaeConsumeActionComponent> ent, ref ComponentStartup args)
    {
        _actionsSystem.AddAction(ent, ref ent.Comp.ConsumeAction, ent.Comp.ConsumeActionId);
    }
}
