using Content.Server._Impstation.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Popups;
using Content.Shared.Storage.EntitySystems;

namespace Content.Server._Impstation.Objectives.Systems;

public sealed class ButlerConditionSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TargetObjectiveSystem _target = default!;
    private static readonly string Slot = "back";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ButlerConditionComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
    }
    private void OnAfterAssign(Entity<ButlerConditionComponent> ent, ref ObjectiveAfterAssignEvent args)
    {
        if (!_target.GetTarget(ent, out var target)) //get the butler target
            return;

        if (!TryComp<MindComponent>(target, out var mind) || mind.CurrentEntity is not { } mindBody)
            return;

        var coords = _transform.GetMapCoordinates(mindBody);
        _popup.PopupEntity(Loc.GetString(ent.Comp.ButlerSpawn), mindBody, mindBody);
        // give the target the remote
        var remote = Spawn(ent.Comp.Package, coords);

        if (!_inventory.TryGetSlotEntity(mindBody, Slot, out var backpack) ||
            !_storage.Insert(backpack.Value, remote, out _)) //bag is full, put in hand
        {
            _hands.TryPickup(mindBody, remote);
            return;
        }
        // no bag somehow, at least pick it up
        _hands.TryPickup(mindBody, remote);
    }
}
