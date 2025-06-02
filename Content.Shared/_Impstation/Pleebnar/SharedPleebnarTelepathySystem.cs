using Content.Shared._Impstation.Pleebnar.Components;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Pleebnar;

[Serializable, NetSerializable]
public enum PleebnarTelepathyUIKey : byte
{
    Key
}
[Serializable, NetSerializable]
public sealed class PleebnarTelepathyBuiState : BoundUserInterfaceState
{
    public readonly string? Vision;

    public PleebnarTelepathyBuiState(string? vision)
    {
        Vision = vision;
    }
}
[Serializable, NetSerializable]
public sealed class PleebnarTelepathyVisionMessage : BoundUserInterfaceMessage
{
    public readonly string? Vision;

    public PleebnarTelepathyVisionMessage(string? vision)
    {
        Vision = vision;
    }
}

public abstract partial class SharedPleebnarTelepathySystem : EntitySystem
{

    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PleebnarTelepathyActionComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<PleebnarTelepathyActionComponent, ComponentShutdown>(OnShutdown);
    }



    public sealed partial class PleebnarTelepathyEvent : EntityTargetActionEvent;
    public sealed partial class PleebnarVisionEvent : InstantActionEvent;
    [Serializable, NetSerializable]
    public sealed partial class PleebnarTelepathyDoAfterEvent : SimpleDoAfterEvent;

    public void OnShutdown(Entity<PleebnarTelepathyActionComponent> ent, ref ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(ent, ent.Comp.TelepathyAction);
        _actionsSystem.RemoveAction(ent, ent.Comp.VisionAction);

    }

    public void OnStartup(Entity<PleebnarTelepathyActionComponent> ent, ref ComponentStartup args)
    {
        _actionsSystem.AddAction(ent, ref ent.Comp.TelepathyAction, ent.Comp.TelepathyActionId);
        _actionsSystem.AddAction(ent, ref ent.Comp.VisionAction, ent.Comp.VisionActionId);
    }
}
