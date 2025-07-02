using Content.Shared.Actions;

namespace Content.Shared._Impstation.NotifierExamine;

public abstract partial class SharedNotifierActionSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<NotifierExamineComponent, ComponentStartup>(OnStartup);
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
    public void OnStartup(Entity<NotifierExamineComponent> ent, ref ComponentStartup args)
    {
        _actionsSystem.AddAction(ent, ref ent.Comp.notifierIconToggle, ent.Comp.notifierIconToggleActionId);
        _actionsSystem.AddAction(ent, ref ent.Comp.notifierToggle, ent.Comp.notifierToggleActionId);
    }
}
