using Content.Server._Impstation.Captain.Components;
using Content.Server.Popups;
using Content.Server.Stunnable;
using Content.Shared._Impstation.Captain;
using Content.Shared.Damage;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Captain.Systems;

/// <summary>
/// <see cref="CaptainSwordMenuComponent"/>
/// this system links the interface to the logic, and will output to the player a set of items selected by him in the interface
/// </summary>
public sealed class CaptainSwordMenuSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioshared = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CaptainSwordMenuComponent, BoundUIOpenedEvent>(OnUIOpened);
        SubscribeLocalEvent<CaptainSwordMenuComponent, CaptainSwordMenuApproveMessage>(OnApprove);
        SubscribeLocalEvent<CaptainSwordMenuComponent, CaptainSwordChangeSetMessage>(OnChangeSet);

    }

    private void OnUIOpened(Entity<CaptainSwordMenuComponent> backpack, ref BoundUIOpenedEvent args)
    {
        UpdateUI(backpack.Owner, backpack.Comp);
    }

    private void OnApprove(Entity<CaptainSwordMenuComponent> backpack, ref CaptainSwordMenuApproveMessage args)
    {
        var soundApprove = new SoundPathSpecifier("/Audio/Effects/holy.ogg");
        if (backpack.Comp.SelectedSets.Count != backpack.Comp.MaxSelectedSets)
            return;

        foreach (var i in backpack.Comp.SelectedSets)
        {
            var set = _proto.Index(backpack.Comp.PossibleSets[i]);
            foreach (var item in set.Content)
            {
                var ent = Spawn(item, _transform.GetMapCoordinates(backpack.Owner));
                if (HasComp<ItemComponent>(ent))
                    _hands.TryPickupAnyHand(args.Actor, ent);
            }
        }
        _audioshared.PlayPvs(soundApprove, args.Actor);
        _popupSystem.PopupEntity(Loc.GetString("captain-sheathe-transformed"), args.Actor, args.Actor);
        QueueDel(backpack);
    }
    private void OnChangeSet(Entity<CaptainSwordMenuComponent> backpack, ref CaptainSwordChangeSetMessage args)
    {
        //Swith selecting set
        if (!backpack.Comp.SelectedSets.Remove(args.SetNumber))
            backpack.Comp.SelectedSets.Add(args.SetNumber);

        UpdateUI(backpack.Owner, backpack.Comp);
    }

    private void UpdateUI(EntityUid uid, CaptainSwordMenuComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        Dictionary<int, CaptainSwordMenuSetInfo> data = new();

        for (int i = 0; i < component.PossibleSets.Count; i++)
        {
            var set = _proto.Index(component.PossibleSets[i]);
            var selected = component.SelectedSets.Contains(i);
            var info = new CaptainSwordMenuSetInfo(
                set.Name,
                set.Description,
                set.Sprite,
                selected);
            data.Add(i, info);
        }

        _ui.SetUiState(uid, CaptainSwordMenuUIKey.Key, new CaptainSwordMenuBoundUserInterfaceState(data, component.MaxSelectedSets));
    }
}
