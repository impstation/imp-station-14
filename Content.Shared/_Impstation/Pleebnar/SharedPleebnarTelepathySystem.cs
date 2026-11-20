using Content.Shared._Impstation.Pleebnar.Components;
using Content.Shared.Actions;
using Content.Shared.Chat;
using Content.Shared.DoAfter;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._Impstation.Pleebnar;

[Serializable, NetSerializable]
public enum PleebnarTelepathyUIKey : byte
{
    Key
}
/// <summary>
/// contains pleebnar telepathy relevant functions needed to be shared across clients and servers
/// </summary>

//message from server to client to determine the new state for the UI
[Serializable, NetSerializable]
public sealed class PleebnarTelepathyBuiState : BoundUserInterfaceState
{
    public readonly string? Vision;

    public PleebnarTelepathyBuiState(string? vision)
    {
        Vision = vision;
    }
}

//message from client to server determined which contains the selected vision
[Serializable, NetSerializable]
public sealed class PleebnarTelepathyVisionMessage : BoundUserInterfaceMessage
{
    public readonly string? Vision;

    public PleebnarTelepathyVisionMessage(string? vision)
    {
        Vision = vision;
    }
}

public partial class SharedPleebnarTelepathySystem : EntitySystem
{

    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedChatSystem _chat = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    //init
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PleebnarTelepathyActionComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<PleebnarTelepathyActionComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<PleebnarTelepathyActionComponent, PleebnarTelepathyEvent>(Telepathy);
        SubscribeLocalEvent<PleebnarTelepathyActionComponent, PleebnarTelepathyDoAfterEvent>(TelepathyDoAfterEvent);
        SubscribeLocalEvent<PleebnarTelepathyActionComponent, PleebnarTelepathyVisionMessage>(OnChangeVision);
        SubscribeLocalEvent<PleebnarTelepathyActionComponent, PleebnarVisionEvent>(OpenUi);
    }
    //event for selecting a receiver
    public sealed partial class PleebnarTelepathyEvent : EntityTargetActionEvent;
    //event for sending a vision after a delay
    [Serializable, NetSerializable]
    public sealed partial class PleebnarTelepathyDoAfterEvent : SimpleDoAfterEvent;
    //event for opening the ui
    public sealed partial class PleebnarVisionEvent : InstantActionEvent;

    //remove actions when component is removed
    public void OnShutdown(Entity<PleebnarTelepathyActionComponent> ent, ref ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(ent.Owner, ent.Comp.TelepathyAction);
        _actionsSystem.RemoveAction(ent.Owner, ent.Comp.VisionAction);

    }

    //add actions when component is added
    public void OnStartup(Entity<PleebnarTelepathyActionComponent> ent, ref ComponentStartup args)
    {
        _actionsSystem.AddAction(ent, ref ent.Comp.TelepathyAction, ent.Comp.TelepathyActionId);
        _actionsSystem.AddAction(ent, ref ent.Comp.VisionAction, ent.Comp.VisionActionId);
    }

        public void Telepathy(Entity<PleebnarTelepathyActionComponent> ent, ref PleebnarTelepathyEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
        {
            return;
        }
        if (!TryComp<MindContainerComponent>(args.Target, out var mind))return; // try to get the mind container, if it fails return
        if (!mind.HasMind)//check if there is an active mind
        {
            _popupSystem.PopupClient(Loc.GetString("pleebnar-telepathy-nomind"), ent, args.Performer,PopupType.SmallCaution);
            return;
        }
        if (ent.Comp.PleebnarVison == null)//check if player has selected a vision
        {
            _popupSystem.PopupClient(Loc.GetString("pleebnar-telepathy-novision"), ent, args.Performer,PopupType.SmallCaution);
            return;
        }

        _popupSystem.PopupPredicted(Loc.GetString("pleebnar-focus"),ent.Owner,ent.Owner);
        var doargs = new DoAfterArgs(EntityManager, ent, 1, new SharedPleebnarTelepathySystem.PleebnarTelepathyDoAfterEvent(), ent, args.Target)
        {
            DistanceThreshold = 5f,
            BreakOnDamage = false,
            BreakOnHandChange = false,
            BreakOnMove = false,
            BreakOnWeightlessMove = false,
            AttemptFrequency = AttemptFrequency.StartAndEnd
        };
        _doAfter.TryStartDoAfter(doargs);
        args.Handled = true;


    }
    //send vision after a delay
    private void TelepathyDoAfterEvent(Entity<PleebnarTelepathyActionComponent> ent,
        ref PleebnarTelepathyDoAfterEvent args)
    {
        if (args.Target == null)//check if target still exists
        {
            return;
        }
        Filter visionAware = Filter.Empty().FromEntities([ent.Owner,(EntityUid)args.Target!]);// filter for chat message, contains the sender and receiver
        _chat.DispatchFilteredAnnouncement(
            visionAware,
            Loc.GetString(ent.Comp.PleebnarVison!),
            ent.Owner,
            Loc.GetString("pleebnar-telepathy-struck")+"\n",
            true,
            ent.Comp.WeirdAudioPath,
            Color.MediumPurple,
            false);
    }
    //handles telling the game to open the ui
    private void OpenUi(Entity<PleebnarTelepathyActionComponent> ent,ref PleebnarVisionEvent args)
    {
        var pleebnar = args.Action.Comp.Container;

        if (!TryComp<PleebnarTelepathyActionComponent>(pleebnar, out var telepathyComp))
            return;

        if (!_uiSystem.HasUi(pleebnar.Value, PleebnarTelepathyUIKey.Key))
            return;

        _uiSystem.OpenUi(pleebnar.Value, PleebnarTelepathyUIKey.Key, args.Performer);
        UpdateUI((pleebnar.Value, telepathyComp));
    }
    //handles sending the game info to update the ui, sends back the selected id pretty much
    private void UpdateUI(Entity<PleebnarTelepathyActionComponent> entity)
    {
        if (_uiSystem.HasUi(entity, PleebnarTelepathyUIKey.Key))
            _uiSystem.SetUiState(entity.Owner, PleebnarTelepathyUIKey.Key, new PleebnarTelepathyBuiState(entity.Comp.PleebnarVisonID));
    }
    //handles setting the selected vision
    private void OnChangeVision(Entity<PleebnarTelepathyActionComponent> entity, ref PleebnarTelepathyVisionMessage msg)
    {
        if(msg.Vision==null)return;
        if (msg.Vision is { } id && !_proto.HasIndex<PleebnarVisionPrototype>(id))
            return;
        var visProto = _proto.Index<PleebnarVisionPrototype>(msg.Vision!);
        entity.Comp.PleebnarVison = visProto.VisionString;
        entity.Comp.PleebnarVisonName = visProto.Name;
        entity.Comp.PleebnarVisonID = visProto.ID;
        _popupSystem.PopupClient(Loc.GetString("pleebnar-telepathy-select",("vision", Loc.GetString(entity.Comp.PleebnarVisonName))),entity.Owner,entity.Owner);
        UpdateUI(entity);
    }

}
