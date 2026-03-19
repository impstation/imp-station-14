using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Server.Tabletop.Components;
using Content.Shared.CCVar;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Tabletop;
using Content.Shared.Tabletop.Components;
using Content.Shared.Tabletop.Events;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using Content.Server.Administration.Logs; //imp
using Content.Shared.Database; //imp
using Content.Shared.Whitelist; //imp

namespace Content.Server.Tabletop
{
    [UsedImplicitly]
    public sealed partial class TabletopSystem : SharedTabletopSystem
    {
        [Dependency] private readonly SharedMapSystem _map = default!;
        [Dependency] private readonly EyeSystem _eye = default!;
        [Dependency] private readonly HandsSystem _hands = default!;
        [Dependency] private readonly ViewSubscriberSystem _viewSubscriberSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IAdminLogManager _adminLog = default!; //imp
        [Dependency] private readonly EntityWhitelistSystem _whitelist = default!; //imp

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<TabletopStopPlayingEvent>(OnStopPlaying);
            SubscribeLocalEvent<TabletopGameComponent, ActivateInWorldEvent>(OnTabletopActivate);
            SubscribeLocalEvent<TabletopGameComponent, ComponentShutdown>(OnGameShutdown);
            SubscribeLocalEvent<TabletopGamerComponent, PlayerDetachedEvent>(OnPlayerDetached);
            SubscribeLocalEvent<TabletopGamerComponent, ComponentShutdown>(OnGamerShutdown);
            SubscribeLocalEvent<TabletopGameComponent, GetVerbsEvent<ActivationVerb>>(AddPlayGameVerb);
            SubscribeLocalEvent<TabletopGameComponent, InteractUsingEvent>(OnInteractUsing);

            SubscribeNetworkEvent<TabletopRequestTakeOut>(OnTabletopRequestTakeOut);

            InitializeMap();
        }

        private void OnTabletopRequestTakeOut(TabletopRequestTakeOut msg, EntitySessionEventArgs args)
        {
            if (args.SenderSession is not { } playerSession)
                return;

            var table = GetEntity(msg.TableUid);

            if (!TryComp(table, out TabletopGameComponent? tabletop) || tabletop.Session is not { } session)
                return;

            if (!msg.Entity.IsValid())
                return;

            var entity = GetEntity(msg.Entity);

            if (!TryComp(entity, out TabletopHologramComponent? hologram))
            {
                _popupSystem.PopupEntity(Loc.GetString("tabletop-error-remove-non-hologram"), table, args.SenderSession);
                return;
            }

            // Check if player is actually playing at this table
            if (!session.Players.ContainsKey(playerSession))
                return;

            // Find the entity, remove it from the session and set it's position to the tabletop
            session.Entities.TryGetValue(entity, out var result);
            session.Entities.Remove(result);
            QueueDel(result);
        }

        private void OnInteractUsing(EntityUid uid, TabletopGameComponent component, InteractUsingEvent args)
        {
            if (!_cfg.GetCVar(CCVars.GameTabletopPlace))
                return;

            if (!TryComp(args.User, out HandsComponent? hands))
                return;

            if (component.Session is not { } session)
                return;

            if (!_hands.TryGetActiveItem((args.User, hands), out var handEnt)) //imp. fixed item hand bug
                return;

            if (!TryComp<ItemComponent>(handEnt, out var item))
                return;

            // imp start

            if (session.Entities.Count >= component.EntMax)
            {
                _popupSystem.PopupEntity(Loc.GetString("tabletop-error-max-ents"), uid, args.User);
                _adminLog.Add(LogType.Action, LogImpact.Low,
                    $"{ToPrettyString(args.User):player} attempted to create a new miniature on game board {ToPrettyString(uid)} and failed!");
                return;
            }

            if (_whitelist.IsWhitelistPass(component.Blacklist, handEnt.Value))
            {
                _popupSystem.PopupEntity(Loc.GetString("tabletop-error-blacklisted"), uid, args.User);
                return;
            }

            // imp end

            var meta = MetaData(handEnt.Value);
            var protoId = meta.EntityPrototype?.ID;

            var hologram = Spawn(protoId, session.Position.Offset(-1, 0));
            _adminLog.Add(LogType.Action, LogImpact.Low, //imp. added logging.
                $"{ToPrettyString(args.User):player} created a new miniature of {ToPrettyString(hologram)} on game board: {ToPrettyString(uid)}");


            // Make sure the entity can be dragged and can be removed, move it into the board game world and add it to the Entities hashmap
            EnsureComp<TabletopDraggableComponent>(hologram);
            EnsureComp<TabletopHologramComponent>(hologram);
            session.Entities.Add(hologram);

            _popupSystem.PopupEntity(Loc.GetString("tabletop-added-piece"), uid, args.User);
        }

        protected override void OnTabletopMove(TabletopMoveEvent msg, EntitySessionEventArgs args)
        {
            if (args.SenderSession is not { } playerSession)
                return;

            if (!TryComp(GetEntity(msg.TableUid), out TabletopGameComponent? tabletop) || tabletop.Session is not { } session)
                return;

            // Check if player is actually playing at this table
            if (!session.Players.ContainsKey(playerSession))
                return;

            base.OnTabletopMove(msg, args);
        }

        /// <summary>
        /// Add verbs that allow the player to start playing a tabletop game, and reset the board. (Imp. changed the comment.)
        /// </summary>
        private void AddPlayGameVerb(EntityUid uid, TabletopGameComponent component, GetVerbsEvent<ActivationVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            if (!TryComp(args.User, out ActorComponent? actor))
                return;

            var playVerb = new ActivationVerb()
            {
                Text = Loc.GetString("tabletop-verb-play-game"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/die.svg.192dpi.png")), //imp. fixed formatting.
                Act = () => OpenSessionFor(actor.PlayerSession, uid),
                Priority = 2 //imp. added priority
            };

            // imp start
            var resetVerb = new ActivationVerb()
            {
                Text = Loc.GetString("tabletop-verb-reset-game"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/refresh.svg.192dpi.png")),
                Act = () => ResetSession(uid),
                Priority = 1
            };
            // imp end

            args.Verbs.Add(playVerb);
            args.Verbs.Add(resetVerb); //imp
        }

        private void OnTabletopActivate(EntityUid uid, TabletopGameComponent component, ActivateInWorldEvent args)
        {
            if (args.Handled || !args.Complex)
                return;

            // Check that a player is attached to the entity.
            if (!TryComp(args.User, out ActorComponent? actor))
                return;

            OpenSessionFor(actor.PlayerSession, uid);
        }

        private void OnGameShutdown(EntityUid uid, TabletopGameComponent component, ComponentShutdown args)
        {
            CleanupSession(uid);
        }

        private void OnStopPlaying(TabletopStopPlayingEvent msg, EntitySessionEventArgs args)
        {
            CloseSessionFor(args.SenderSession, GetEntity(msg.TableUid));
        }

        private void OnPlayerDetached(EntityUid uid, TabletopGamerComponent component, PlayerDetachedEvent args)
        {
            if(component.Tabletop.IsValid())
                CloseSessionFor(args.Player, component.Tabletop);
        }

        private void OnGamerShutdown(EntityUid uid, TabletopGamerComponent component, ComponentShutdown args)
        {
            if (!TryComp(uid, out ActorComponent? actor))
                return;

            if(component.Tabletop.IsValid())
                CloseSessionFor(actor.PlayerSession, component.Tabletop);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<TabletopGamerComponent>();
            while (query.MoveNext(out var uid, out var gamer))
            {
                if (!Exists(gamer.Tabletop))
                    continue;

                if (!TryComp(uid, out ActorComponent? actor))
                {
                    RemComp<TabletopGamerComponent>(uid);
                    return;
                }

                if (actor.PlayerSession.Status != SessionStatus.InGame || !CanSeeTable(uid, gamer.Tabletop))
                    CloseSessionFor(actor.PlayerSession, gamer.Tabletop);
            }
        }
    }
}
