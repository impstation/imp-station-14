using Content.Shared.Inventory;
using Content.Shared.Storage.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;
// imp add start
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Utility;
// imp end

namespace Content.Shared.Storage.EntitySystems;

/// <summary>
/// <see cref="MagnetPickupComponent"/>
/// </summary>
public sealed class MagnetPickupSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!; // imp


    private static readonly TimeSpan ScanDelay = TimeSpan.FromSeconds(1);

    private EntityQuery<PhysicsComponent> _physicsQuery;

    private static readonly string MagnetVerbIcon = "/Textures/Interface/VerbIcons/Spare/poweronoff.svg.192dpi.png";

    public override void Initialize()
    {
        base.Initialize();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        SubscribeLocalEvent<MagnetPickupComponent, MapInitEvent>(OnMagnetMapInit);
        SubscribeLocalEvent<MagnetPickupComponent, ExaminedEvent>(OnExamined); // imp
        SubscribeLocalEvent<MagnetPickupComponent, GetVerbsEvent<AlternativeVerb>>(AddToggleMagnetVerb); // imp
    }

    private void OnMagnetMapInit(EntityUid uid, MagnetPickupComponent component, MapInitEvent args)
    {
        component.NextScan = _timing.CurTime;
    }

    // imp. used to add the magnet toggle to the context menu
    private void AddToggleMagnetVerb(EntityUid uid, MagnetPickupComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!HasComp<HandsComponent>(args.User))
            return;

        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                ToggleMagnet(uid, component, args);
            },
            Icon = new SpriteSpecifier.Texture(new(MagnetVerbIcon)),
            Text = Loc.GetString("magnet-pickup-component-toggle-verb"),
            Priority = 3
        };

        args.Verbs.Add(verb);
    }

    // used to show the magnet state on examination
    private void OnExamined(EntityUid uid, MagnetPickupComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("magnet-pickup-component-on-examine-main",
                        ("stateText", Loc.GetString(component.MagnetEnabled
                        ? "magnet-pickup-component-magnet-on"
                        : "magnet-pickup-component-magnet-off"))));
    }

    // used to toggle the magnet on the ore bag/box
    public bool ToggleMagnet(EntityUid uid, MagnetPickupComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        _popup.PopupPredicted(Loc.GetString(comp.MagnetEnabled
                            ? "magnet-pickup-component-toggle-off"
                            : "magnet-pickup-component-toggle-on"), uid, args.User);
        comp.MagnetEnabled = !comp.MagnetEnabled;
        Dirty(uid, comp);
        return comp.MagnetEnabled;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<MagnetPickupComponent, StorageComponent, TransformComponent, MetaDataComponent>();
        var currentTime = _timing.CurTime;

        while (query.MoveNext(out var uid, out var comp, out var storage, out var xform, out var meta))
        {
            if (comp.NextScan > currentTime)
                continue;

            comp.NextScan += ScanDelay;
            Dirty(uid, comp);

            if (!_inventory.TryGetContainingSlot((uid, xform, meta), out var slotDef))
                continue;

            if ((slotDef.SlotFlags & comp.SlotFlags) == 0x0)
                continue;

            // No space
            if (!_storage.HasSpace((uid, storage)))
                continue;

            // imp add start
            // magnet disabled
            if (!comp.MagnetEnabled)
                continue;

            var parentUid = xform.ParentUid;
            var playedSound = false;
            var finalCoords = xform.Coordinates;
            var moverCoords = _transform.GetMoverCoordinates(uid, xform);

            foreach (var near in _lookup.GetEntitiesInRange(uid, comp.Range, LookupFlags.Dynamic | LookupFlags.Sundries))
            {
                if (_whitelistSystem.IsWhitelistFail(storage.Whitelist, near))
                    continue;

                if (!_physicsQuery.TryGetComponent(near, out var physics) || physics.BodyStatus != BodyStatus.OnGround)
                    continue;

                if (near == parentUid)
                    continue;

                // TODO: Probably move this to storage somewhere when it gets cleaned up
                // TODO: This sucks but you need to fix a lot of stuff to make it better
                // the problem is that stack pickups delete the original entity, which is fine, but due to
                // game state handling we can't show a lerp animation for it.
                var nearXform = Transform(near);
                var nearMap = _transform.GetMapCoordinates(near, xform: nearXform);
                var nearCoords = _transform.ToCoordinates(moverCoords.EntityId, nearMap);

                if (!_storage.Insert(uid, near, out var stacked, storageComp: storage, playSound: !playedSound))
                    continue;

                // Play pickup animation for either the stack entity or the original entity.
                if (stacked != null)
                    _storage.PlayPickupAnimation(stacked.Value, nearCoords, finalCoords, nearXform.LocalRotation);
                else
                    _storage.PlayPickupAnimation(near, nearCoords, finalCoords, nearXform.LocalRotation);

                playedSound = true;
            }
        }
    }
}
