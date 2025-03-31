using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Kodepiia;

public abstract partial class SharedKodepiiaConsumeSystem : EntitySystem
{
    public sealed partial class KodepiiaConsumeEvent : EntityTargetActionEvent;

    [Serializable, NetSerializable]
    public sealed partial class KodepiiaConsumeDoAfterEvent : SimpleDoAfterEvent;

    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    public void OnShutdown(Entity<Components.KodepiiaConsumeActionComponent> ent, ref ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(ent, ent.Comp.ConsumeAction);
    }

    public void OnStartup(Entity<Components.KodepiiaConsumeActionComponent> ent, ref ComponentStartup args)
    {
        _actionsSystem.AddAction(ent, ref ent.Comp.ConsumeAction, ent.Comp.ConsumeActionId);
    }
}
