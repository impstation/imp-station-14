using Content.Server._Impstation.Objectives.Components;
using Content.Server.DeviceLinking.Components;
using Content.Server.Objectives.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Implants.Components;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Server.Audio;
using Robust.Shared.Player;

namespace Content.Server._Impstation.Objectives.Systems;

/// <summary>
/// Finds the exploding butler target and spawns a care package at their location.
/// </summary>
public sealed class ButlerConditionSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
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

    /// <summary>
    /// Finds the exploding butler target and spawns a care package at their location alongside removing autolinking.
    /// </summary>
    private void OnAfterAssign(Entity<ButlerConditionComponent> ent, ref ObjectiveAfterAssignEvent args)
    {
        if (!_target.GetTarget(ent, out var target)) // get the butler target
            return;

        if (!TryComp<MindComponent>(target, out var mind) || mind.CurrentEntity is not { } mindBody)
            return;

        if (!_player.TryGetSessionById(mind.UserId, out var session))
            return;

        _popup.PopupEntity(Loc.GetString(ent.Comp.ButlerSpawn), mindBody, mindBody);
        _audio.PlayGlobal(ent.Comp.TargetAudio, session);

        // give the target the remote
        var coords = _transform.GetMapCoordinates(mindBody);
        var package = Spawn(ent.Comp.Package, coords);

        if (args.Mind.CurrentEntity is { })
            RemCompDeferred<AutoLinkReceiverComponent>(args.Mind.CurrentEntity.Value);

        if (!TryComp<ImplantedComponent>(args.Mind.CurrentEntity, out var implanted))
            return;

        foreach (var implant in implanted.ImplantContainer.ContainedEntities)
            RemCompDeferred<AutoLinkReceiverComponent>(implant);

        if (!_inventory.TryGetSlotEntity(mindBody, Slot, out var backpack) ||
            !_storage.Insert(backpack.Value, package, out _)) //bag is full, put in hand
        {
            _hands.TryPickup(mindBody, package);
            return;
        }
        // no bag somehow, at least pick it up
        _hands.TryPickup(mindBody, package);
    }
}
