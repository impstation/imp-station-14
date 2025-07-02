

using Content.Shared._Impstation.NotifierExamine;


namespace Content.Server._Impstation.NotifierExamine;

public sealed class NotifierActionSystem : SharedNotifierActionSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NotifierExamineComponent, NotifierIconToggleEvent>(ToggleIcon);
        SubscribeLocalEvent<NotifierExamineComponent, NotifierToggleEvent>(ToggleNotifier);
    }
    private void ToggleIcon(Entity<NotifierExamineComponent> ent,ref NotifierIconToggleEvent args)
    {
        ent.Comp.IconOn = !ent.Comp.IconOn;

    }
    private void ToggleNotifier(Entity<NotifierExamineComponent> ent,ref NotifierToggleEvent args)
    {
        ent.Comp.Active = !ent.Comp.Active;
    }
}
