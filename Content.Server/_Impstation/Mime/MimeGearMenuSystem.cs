using Content.Shared._Impstation.Mime;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Mime;

/// <summary>
/// <see cref="MimeGearMenuComponent"/>
/// this system links the interface to the logic, and will output to the player a set of items selected by him in the interface
/// </summary>
public sealed class MimeGearMenuSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MimeGearMenuComponent, BoundUIOpenedEvent>(OnUIOpened);
        SubscribeLocalEvent<MimeGearMenuComponent, MimeGearMenuApproveMessage>(OnApprove);
        SubscribeLocalEvent<MimeGearMenuComponent, MimeGearChangeSetMessage>(OnChangeSet);
    }

    private void OnUIOpened(Entity<MimeGearMenuComponent> backpack, ref BoundUIOpenedEvent args)
    {
        UpdateUI(backpack.Owner, backpack.Comp);
    }

    private void OnApprove(Entity<MimeGearMenuComponent> backpack, ref MimeGearMenuApproveMessage args)
    {
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
        QueueDel(backpack);
    }
    private void OnChangeSet(Entity<MimeGearMenuComponent> backpack, ref MimeGearChangeSetMessage args)
    {
        // Switch selecting set
        if (!backpack.Comp.SelectedSets.Remove(args.SetNumber))
            backpack.Comp.SelectedSets.Add(args.SetNumber);

        UpdateUI(backpack.Owner, backpack.Comp);
    }

    private void UpdateUI(EntityUid uid, MimeGearMenuComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        Dictionary<int, MimeGearMenuSetInfo> data = new();

        for (var i = 0; i < component.PossibleSets.Count; i++)
        {
            var set = _proto.Index(component.PossibleSets[i]);
            var selected = component.SelectedSets.Contains(i);
            var info = new MimeGearMenuSetInfo(
                set.Name,
                set.Description,
                set.Sprite,
                selected);
            data.Add(i, info);
        }

        _ui.SetUiState(uid, MimeGearMenuUIKey.Key, new MimeGearMenuBoundUserInterfaceState(data, component.MaxSelectedSets));
    }
}
