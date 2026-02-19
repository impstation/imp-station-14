using Content.Shared.Popups;
using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Content.Shared.Alert;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.IdentityManagement;
using Content.Shared.Maps;
using Content.Shared.Paper;
using Content.Shared.Physics;
using Content.Shared.Speech.Muting;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Content.Shared.Ghost; // imp
using Content.Shared.Mind; // imp
using Content.Shared.Mind.Components; // imp

namespace Content.Shared.Abilities.Mime;

public sealed class MimePowersSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!; // imp

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MimePowersComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<MimePowersComponent, ComponentShutdown>(OnComponentShutdown);
        //SubscribeLocalEvent<MimePowersComponent, InvisibleWallActionEvent>(OnInvisibleWall); // imp edit

        SubscribeLocalEvent< /*MimePowersComponent, */BreakVowAlertEvent>(OnBreakVowAlert); // imp edit
        SubscribeLocalEvent</*MimePowersComponent, */RetakeVowAlertEvent>(OnRetakeVowAlert); // imp edit

        SubscribeLocalEvent<MimePowersComponent, MindGotAddedEvent>(OnMindGotAdded); // imp edit
        SubscribeLocalEvent<MimePowersComponent, MindGotRemovedEvent>(OnMindGotRemoved); // imp edit
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        // Queue to track whether mimes can retake vows yet

        var query = EntityQueryEnumerator<MimePowersComponent>();
        while (query.MoveNext(out var uid, out var mime))
        {
            // imp edit start
            if (!TryComp<MindComponent>(uid, out var mind))
                continue;
            // imp edit end

            if (!mime.VowBroken || mime.ReadyToRepent)
                continue;

            if (_timing.CurTime < mime.VowRepentTime)
                continue;

            mime.ReadyToRepent = true;
            Dirty(uid, mime);

            // imp edit start
            // _popupSystem.PopupClient(Loc.GetString("mime-ready-to-repent"), uid, uid);
            if (mind.CurrentEntity != null)
                _popupSystem.PopupClient(Loc.GetString("mime-ready-to-repent"), mind.CurrentEntity.Value, mind.CurrentEntity.Value);
            // imp edit end
        }
    }

    private void OnComponentInit(Entity<MimePowersComponent> ent, ref ComponentInit args)
    {
        // imp edit start
        if (!TryComp<MindComponent>(ent, out var mind))
            return;

        if (mind.CurrentEntity == null)
            return;

        var mutedComponent = EnsureComp<MutedComponent>(mind.CurrentEntity.Value);
        mutedComponent.MutedScream = false;
        // imp edit end

        if (ent.Comp.PreventWriting)
        {
            EnsureComp<BlockWritingComponent>(mind.CurrentEntity.Value, out var illiterateComponent); // imp edit, ent -> mind.CurrentEntity.Value
            illiterateComponent.FailWriteMessage = ent.Comp.FailWriteMessage;
            Dirty(mind.CurrentEntity.Value, illiterateComponent); // imp edit, ent -> mind.CurrentEntity.Value
        }

        _alertsSystem.ShowAlert(mind.CurrentEntity.Value, ent.Comp.VowAlert); // imp edit, ent.Owner -> mind.CurrentEntity.Value
        //_actionsSystem.AddAction(ent, ref ent.Comp.InvisibleWallActionEntity, ent.Comp.InvisibleWallAction); // imp edit
    }

    private void OnComponentShutdown(Entity<MimePowersComponent> ent, ref ComponentShutdown args)
    {
        //_actionsSystem.RemoveAction(ent.Owner, ent.Comp.InvisibleWallActionEntity); // imp edit

        // imp edit start
        if (!TryComp<MindComponent>(ent, out var mind))
            return;

        if (mind.CurrentEntity == null)
            return;

        RemComp<MutedComponent>(mind.CurrentEntity.Value);
        if (ent.Comp.PreventWriting)
            RemComp<BlockWritingComponent>(mind.CurrentEntity.Value);

        _alertsSystem.ClearAlert(mind.CurrentEntity.Value, ent.Comp.VowAlert);
        _alertsSystem.ClearAlert(mind.CurrentEntity.Value, ent.Comp.VowBrokenAlert);
        // imp edit end
    }

    /* imp edit
    /// <summary>
    /// Creates an invisible wall in a free space after some checks.
    /// </summary>
    private void OnInvisibleWall(Entity<MimePowersComponent> ent, ref InvisibleWallActionEvent args)
    {
        if (!ent.Comp.Enabled)
            return;

        if (_container.IsEntityOrParentInContainer(ent))
            return;

        var xform = Transform(ent);
        // Get the tile in front of the mime
        var offsetValue = xform.LocalRotation.ToWorldVec();
        var coords = xform.Coordinates.Offset(offsetValue).SnapToGrid(EntityManager, _mapMan);
        var tile = _turf.GetTileRef(coords);
        if (tile == null)
            return;

        // Check if the tile is blocked by a wall or mob, and don't create the wall if so
        if (_turf.IsTileBlocked(tile.Value, CollisionGroup.Impassable | CollisionGroup.Opaque))
        {
            _popupSystem.PopupClient(Loc.GetString("mime-invisible-wall-failed"), ent, ent);
            return;
        }

        var messageSelf = Loc.GetString("mime-invisible-wall-popup-self", ("mime", Identity.Entity(ent.Owner, EntityManager)));
        var messageOthers = Loc.GetString("mime-invisible-wall-popup-others", ("mime", Identity.Entity(ent.Owner, EntityManager)));
        _popupSystem.PopupPredicted(messageSelf, messageOthers, ent, ent);

        // Make sure we set the invisible wall to despawn properly
        PredictedSpawnAtPosition(ent.Comp.WallPrototype, _turf.GetTileCenter(tile.Value));
        // Handle args so cooldown works
        args.Handled = true;
    }
    */

    private void OnBreakVowAlert(/*Entity<MimePowersComponent> ent, ref */BreakVowAlertEvent args) // imp edit
    {
        if (args.Handled)
            return;

        //BreakVow(ent, ent); // imp edit
        BreakVow(args.User); // imp edit
        args.Handled = true;
    }

    private void OnRetakeVowAlert(/*Entity<MimePowersComponent> ent, ref */RetakeVowAlertEvent args) // imp edit
    {
        if (args.Handled)
            return;

        //RetakeVow(ent, ent); // imp edit
        RetakeVow(args.User); // imp edit
        args.Handled = true;
    }

    /// <summary>
    /// Break this mime's vow to not speak.
    /// </summary>
    public void BreakVow(EntityUid uid, MimePowersComponent? mimePowers = null)
    {
        // imp edit start
        var mind = _mind.GetMind(uid);

        if (mind == null)
            return;
        // imp edit end

        if (!Resolve(mind.Value, ref mimePowers)) // imp edit, uid -> mind.Value
            return;

        if (mimePowers.VowBroken)
            return;

        mimePowers.Enabled = false;
        mimePowers.VowBroken = true;
        mimePowers.VowRepentTime = _timing.CurTime + mimePowers.VowCooldown;
        Dirty(mind.Value, mimePowers); // imp edit, uid -> mind.Value
        RemComp<MutedComponent>(uid);
        if (mimePowers.PreventWriting)
            RemComp<BlockWritingComponent>(uid);

        _alertsSystem.ClearAlert(uid, mimePowers.VowAlert);
        _alertsSystem.ShowAlert(uid, mimePowers.VowBrokenAlert);
        //_actionsSystem.RemoveAction(uid, mimePowers.InvisibleWallActionEntity); // imp edit
    }

    /// <summary>
    /// Retake this mime's vow to not speak.
    /// </summary>
    public void RetakeVow(EntityUid uid, MimePowersComponent? mimePowers = null)
    {
        // imp edit start
        var mind = _mind.GetMind(uid);

        if (mind == null)
            return;
        // imp edit end

        if (!Resolve(mind.Value, ref mimePowers)) // imp edit, uid -> mind.Value
            return;

        if (!mimePowers.ReadyToRepent)
        {
            _popupSystem.PopupClient(Loc.GetString("mime-not-ready-repent"), uid, uid);
            return;
        }

        mimePowers.Enabled = true;
        mimePowers.ReadyToRepent = false;
        mimePowers.VowBroken = false;
        Dirty(mind.Value, mimePowers); // imp edit, uid -> mind.Value
        //AddComp<MutedCaomponent>(uid); // imp edit
        // imp edit start
        var mutedComponent = EnsureComp<MutedComponent>(uid);
        mutedComponent.MutedScream = false;
        // imp edit end
        if (mimePowers.PreventWriting)
        {
            EnsureComp<BlockWritingComponent>(uid, out var illiterateComponent);
            illiterateComponent.FailWriteMessage = mimePowers.FailWriteMessage;
            Dirty(uid, illiterateComponent);
        }

        _alertsSystem.ClearAlert(uid, mimePowers.VowBrokenAlert);
        _alertsSystem.ShowAlert(uid, mimePowers.VowAlert);
        //_actionsSystem.AddAction(uid, ref mimePowers.InvisibleWallActionEntity, mimePowers.InvisibleWallAction, uid); // imp edit
    }

    // imp edit start
    private void OnMindGotAdded(Entity<MimePowersComponent> ent, ref MindGotAddedEvent args)
    {
        if (HasComp<GhostComponent>(args.Container))
            return;

        if (ent.Comp.Enabled)
        {
            var mutedComponent = EnsureComp<MutedComponent>(args.Container.Owner);
            mutedComponent.MutedScream = false;

            if (ent.Comp.PreventWriting)
            {
                EnsureComp<BlockWritingComponent>(args.Container.Owner, out var illiterateComponent);
                illiterateComponent.FailWriteMessage = ent.Comp.FailWriteMessage;
                Dirty(args.Container.Owner, illiterateComponent);
            }

            _alertsSystem.ShowAlert(args.Container.Owner, ent.Comp.VowAlert);
        }
        else
        {
            _alertsSystem.ShowAlert(args.Container.Owner, ent.Comp.VowBrokenAlert);
        }
    }

    private void OnMindGotRemoved(Entity<MimePowersComponent> ent, ref MindGotRemovedEvent args)
    {
        RemComp<MutedComponent>(args.Container.Owner);
        if (ent.Comp.PreventWriting)
            RemComp<BlockWritingComponent>(args.Container.Owner);

        _alertsSystem.ClearAlert(args.Container.Owner, ent.Comp.VowAlert);
        _alertsSystem.ClearAlert(args.Container.Owner, ent.Comp.VowBrokenAlert);
    }
    // imp edit end
}
