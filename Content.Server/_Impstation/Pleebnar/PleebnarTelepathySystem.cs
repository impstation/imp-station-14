using Content.Server.Administration;
using Content.Server.DoAfter;
using Content.Shared._Impstation.Pleebnar;
using Content.Shared._Impstation.Pleebnar.Components;
using Content.Shared.Chat;
using Content.Shared.DoAfter;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Speech;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Pleebnar;

public sealed class PleebnarTelepathySystem : SharedPleebnarTelepathySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PleebnarTelepathyActionComponent, PleebnarTelepathyEvent>(Telepathy);
        SubscribeLocalEvent<PleebnarTelepathyActionComponent, PleebnarTelepathyVisionMessage>(OnChangeVision);
        SubscribeLocalEvent<PleebnarTelepathyActionComponent, PleebnarVisionEvent>(OpenUi);
    }

    public void Telepathy(Entity<PleebnarTelepathyActionComponent> ent, ref PleebnarTelepathyEvent args)
    {


        if(!TryComp<MindContainerComponent>(args.Target, out var mind)) return;
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
        _popupSystem.PopupEntity(ent.Comp.PleebnarVison,args.Target,ent,PopupType.Large);
        _popupSystem.PopupEntity(ent.Comp.PleebnarVison,args.Target,args.Target,PopupType.Large);
        args.Handled = true;

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
            _uiSystem.SetUiState(entity.Owner, PleebnarTelepathyUIKey.Key, new PleebnarTelepathyBuiState(entity.Comp.PleebnarVison));
    }
    private void OnChangeVision(Entity<PleebnarTelepathyActionComponent> entity, ref PleebnarTelepathyVisionMessage msg)
    {
        if (msg.Vision is { } id && !_proto.HasIndex<PleebnarVisionPrototype>(id))
            return;

        entity.Comp.PleebnarVison = msg.Vision;


        UpdateUI(entity);
    }

}
