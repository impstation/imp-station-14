using Content.Shared._Impstation.CCVar;
using Content.Shared.Actions;
using Content.Shared.GameTicking;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Shared._Impstation.NotifierExamine;

public abstract partial class SharedNotifierActionSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<NotifierExamineComponent, ComponentShutdown>(OnShutdown);
    }
    public sealed partial class  NotifierIconToggleEvent : InstantActionEvent;
    public sealed partial class  NotifierToggleEvent : InstantActionEvent;
    public void OnShutdown(Entity<NotifierExamineComponent> ent, ref ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(ent.Owner,ent.Comp.notifierIconToggle);
        _actionsSystem.RemoveAction(ent.Owner,ent.Comp.notifierToggle);

    }
    //add actions when component is added

}
