using Content.Server.Antag;
using Content.Server.CharacterAppearance.Components;
using Content.Server.Objectives.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Storage.EntitySystems;

namespace Content.Server.GameTicking.Rules.Components;

public sealed class ButlerRuleSystem : GameRuleSystem<ButlerRuleComponent>
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ButlerRuleComponent, AfterAntagEntitySelectedEvent>(AfterAntagEntitySelected);
    }
    private void AfterAntagEntitySelected(Entity<ButlerRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        if (!TryComp<TargetObjectiveComponent>(ent, out var targetComp)) //get the butler target
            return;

        if (targetComp.Target is not { } target) //nullcheck?
            return;

        var coords = _transform.GetMapCoordinates(target);
        _popup.PopupEntity(Loc.GetString("butler-spawn"), target, target);
        // give the target the remote
        var remote = Spawn(ent.Comp.Signaller, coords);

        // try to insert it into their bag
        if (_inventory.TryGetSlotEntity(target, "back", out var backpack))
        {
            _storage.Insert(backpack.Value, remote, out _);
        }
        else
        {
            // no bag somehow, at least pick it up
            _hands.TryPickup(target, remote);
        }
    }
}
