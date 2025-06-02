using System.Diagnostics;
using Content.Server.Administration;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.DoAfter;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Shared._Impstation.Pleebnar;
using Content.Shared._Impstation.Pleebnar.Components;
using Content.Shared.Chat;
using Content.Shared.DoAfter;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Speech;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Pleebnar;

public sealed class PleebnarTelepathySystem : SharedPleebnarTelepathySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PleebnarTelepathyActionComponent, PleebnarTelepathyEvent>(Telepathy);
        SubscribeLocalEvent<PleebnarTelepathyActionComponent, PleebnarTelepathyDoAfterEvent>(TelepathyDoAfterEvent);
        SubscribeLocalEvent<PleebnarTelepathyActionComponent, PleebnarTelepathyVisionMessage>(OnChangeVision);
        SubscribeLocalEvent<PleebnarTelepathyActionComponent, PleebnarVisionEvent>(OpenUi);

    }

    public void Telepathy(Entity<PleebnarTelepathyActionComponent> ent, ref PleebnarTelepathyEvent args)
    {
        if (!TryComp<MindContainerComponent>(args.Target, out var mind))return;
        if (!mind.HasMind)
        {
            _popupSystem.PopupEntity(Loc.GetString("pleebnar-telepathy-nomind"), ent, args.Performer,PopupType.SmallCaution);
            return;
        }
        if (ent.Comp.PleebnarVison == null)
        {
            _popupSystem.PopupEntity(Loc.GetString("pleebnar-telepathy-novision"), ent, args.Performer,PopupType.SmallCaution);
            return;
        }




        //_popupSystem.PopupEntity(Loc.GetString("pleebnar-telepathy-struck"),args.Target,ent,PopupType.Large);
        //_popupSystem.PopupEntity(Loc.GetString("pleebnar-telepathy-struck"),args.Target,args.Target,PopupType.Large);
        //_popupSystem.PopupEntity(ent.Comp.PleebnarVison,args.Target,ent,PopupType.Large);
        //_popupSystem.PopupEntity(ent.Comp.PleebnarVison,args.Target,args.Target,PopupType.Large);

        _popupSystem.PopupEntity(Loc.GetString("pleebnar-focus"),ent.Owner,PopupType.Small);
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

    private void TelepathyDoAfterEvent(Entity<PleebnarTelepathyActionComponent> ent,
        ref PleebnarTelepathyDoAfterEvent args)
    {
        if (args.Target == null)
        {
            return;
        }
        Filter visionAware = Filter.Empty().FromEntities([ent.Owner,(EntityUid)args.Target!]);
        _chatManager.ChatMessageToManyFiltered(
            visionAware,
            ChatChannel.Notifications,
            Loc.GetString("pleebnar-telepathy-struck")+"\n"+Loc.GetString(ent.Comp.PleebnarVison!),
            Loc.GetString("pleebnar-telepathy-struck")+"\n"+Loc.GetString(ent.Comp.PleebnarVison!),
            (EntityUid)args.Target!,
            false,
            true,
            Color.MediumPurple,
            ent.Comp.announceAudioPath,
            2f);
    }
    private void OpenUi(Entity<PleebnarTelepathyActionComponent> ent,ref PleebnarVisionEvent args)
    {
        var maskEntity = args.Action.Comp.Container;

        if (!TryComp<PleebnarTelepathyActionComponent>(maskEntity, out var telepathyComp))
            return;

        if (!_uiSystem.HasUi(maskEntity.Value, PleebnarTelepathyUIKey.Key))
            return;

        _uiSystem.OpenUi(maskEntity.Value, PleebnarTelepathyUIKey.Key, args.Performer);
        UpdateUI((maskEntity.Value, telepathyComp));
    }
    private void UpdateUI(Entity<PleebnarTelepathyActionComponent> entity)
    {
        if (_uiSystem.HasUi(entity, PleebnarTelepathyUIKey.Key))
            _uiSystem.SetUiState(entity.Owner, PleebnarTelepathyUIKey.Key, new PleebnarTelepathyBuiState(Loc.GetString(entity.Comp.PleebnarVisonName??"pleebnar-vision-none-name")));
    }
    private void OnChangeVision(Entity<PleebnarTelepathyActionComponent> entity, ref PleebnarTelepathyVisionMessage msg)
    {
        if(msg.Vision==null)return;
        if (msg.Vision is { } id && !_proto.HasIndex<PleebnarVisionPrototype>(id))
            return;
        var visProto = _proto.Index<PleebnarVisionPrototype>(msg.Vision!);
        entity.Comp.PleebnarVison = visProto.VisionString;
        entity.Comp.PleebnarVisonName = visProto.Name;

        UpdateUI(entity);
    }

}
