using Content.Server._Impstation.StationEvents.Components;
using Content.Server.Antag;
using Content.Server.Chat.Managers;
using Content.Server.Objectives.Components;
using Content.Server.Pinpointer;
using Content.Server.StationEvents.Events;
using Content.Shared._Impstation.BloodlessChimp.Components;
using Content.Shared.Chat;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Robust.Server.Player;
using Robust.Shared.Audio;

namespace Content.Server._Impstation.StationEvents.Events;

public sealed class BloodlessChimpRule : StationEventSystem<BloodlessChimpRuleComponent>
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly PinpointerSystem _pinpointer = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodlessChimpRuleComponent, AfterAntagEntitySelectedEvent>(AfterAntagEntitySelected);

    }

    private void AfterAntagEntitySelected(Entity<BloodlessChimpRuleComponent> ent,
        ref AfterAntagEntitySelectedEvent args)
    {
        if(!_mind.TryGetMind(args.EntityUid,out var chimpMindId, out var chimpMind))
            return;
        if(!_mind.TryGetObjectiveComp<TargetObjectiveComponent>(chimpMindId, out var targetComp,chimpMind))
            return;
        if((targetComp.Target == null) || !_entityManager.TryGetComponent<MindComponent>(targetComp.Target, out var targetMind))
            return;
        if (!_playerManager.TryGetSessionById(targetMind.UserId, out var targetSession))
            return;
        if (!_playerManager.TryGetSessionById(chimpMind.UserId, out var chimpSession))
            return;
        SoundSpecifier chimpAnnounce = new SoundPathSpecifier("/Audio/_Impstation/Effects/chimpAnnounce.ogg");


        _chatManager.ChatMessageToOne(ChatChannel.Notifications,
            Loc.GetString(ent.Comp.TargetAnnouncement),
            Loc.GetString(ent.Comp.TargetAnnouncement),
            targetComp.Target.Value,
            false,
            targetSession.Channel,
            Color.Red,
            audioPath: "/Audio/_Impstation/Effects/chimpAnnounce.ogg"
        );

        _antag.SendBriefing(chimpSession,Loc.GetString(ent.Comp.ChimpAnnouncement,("target", targetMind.CharacterName ?? "your target")),Color.Red, chimpAnnounce);


        if(!_inventory.TryGetSlotContainer(args.EntityUid,"pocket1",out var pocket1,out var _))
            return;

        var pinpointer = _slots.GetItemOrNull(args.EntityUid,"pocket1");
        if (pocket1.ContainedEntity == null)
            return;

        _pinpointer.SetTarget(pocket1.ContainedEntity.Value, targetMind.CurrentEntity);

        EnsureComp<FearRadiusComponent>(args.EntityUid,out var fearRadius);

        fearRadius.Radius = 5f;
        fearRadius.Target = targetMind.CurrentEntity;


    }
}
