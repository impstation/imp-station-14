using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Kodepiia;

public abstract partial class SharedKodepiiaeConsumeSystem : EntitySystem
{
    public sealed partial class KodepiiaeConsumeEvent : EntityTargetActionEvent;

    [Serializable, NetSerializable]
    public sealed partial class KodepiiaeConsumeDoAfterEvent : SimpleDoAfterEvent;

    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    public void OnShutdown(Entity<Kodepiia.Components.KodepiiaeConsumeActionComponent> ent, ref ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(ent, ent.Comp.ConsumeAction);
    }
    public void OnStartup(Entity<Kodepiia.Components.KodepiiaeConsumeActionComponent> ent, ref ComponentStartup args)
    {
        _actionsSystem.AddAction(ent, ref ent.Comp.ConsumeAction, ent.Comp.ConsumeActionId);
    }
}
