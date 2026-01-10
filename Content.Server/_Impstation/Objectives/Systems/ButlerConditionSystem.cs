using Content.Server.Objectives.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Popups;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Toolshed.Commands.Values;

namespace Content.Server.Objectives.Systems;

public sealed class ButlerConditionSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TargetObjectiveSystem _target = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ButlerConditionComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
    }
    private void OnAfterAssign(EntityUid uid, ButlerConditionComponent comp, ref ObjectiveAfterAssignEvent args)
    {
        if (!_target.GetTarget(uid, out var target)) //get the butler target
            return;

        if (!TryComp<MindComponent>(target, out var mind) || mind.CurrentEntity is not { } mindBody)
            return;

        var coords = _transform.GetMapCoordinates(mindBody);
        _popup.PopupEntity(Loc.GetString("butler-spawn"), mindBody, mindBody);
        // give the target the remote
        var remote = Spawn(comp.Signaller, coords);

        // try to insert it into their bag (thank you fugitive system)
        if (_inventory.TryGetSlotEntity(mindBody, "back", out var backpack))
        {
            _storage.Insert(backpack.Value, remote, out _);
        }
        else
        {
            // no bag somehow, at least pick it up
            _hands.TryPickup(mindBody, remote);
        }
    }
}
