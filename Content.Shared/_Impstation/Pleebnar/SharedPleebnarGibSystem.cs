using Content.Shared._Impstation.Pleebnar.Components;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Pleebnar;

public abstract partial class SharedPleebnarGibSystem : EntitySystem
{

    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PleebnarGibActionComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<PleebnarGibActionComponent, ComponentShutdown>(OnShutdown);
    }

    public sealed partial class PleebnarGibEvent : EntityTargetActionEvent;
    [Serializable, NetSerializable]
    public sealed partial class PleebnarGibDoAfterEvent : SimpleDoAfterEvent;
    public void OnShutdown(Entity<PleebnarGibActionComponent> ent, ref ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(ent, ent.Comp.gibAction);

    }

    public void OnStartup(Entity<PleebnarGibActionComponent> ent, ref ComponentStartup args)
    {
        _actionsSystem.AddAction(ent, ref ent.Comp.gibAction, ent.Comp.gibActionId);
    }
}
