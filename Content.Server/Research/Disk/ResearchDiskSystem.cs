using System.Linq;
using Content.Shared.Interaction;
using Content.Server.Popups;
using Content.Shared.Research.Prototypes;
using Content.Server.Research.Systems;
using Content.Shared.Research.Components;
using Robust.Shared.Prototypes;
using Content.Server.Research.Components; // imp
using Robust.Shared.Audio; // imp
using Robust.Shared.Audio.Systems; // imp

namespace Content.Server.Research.Disk
{
    public sealed class ResearchDiskSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototype = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly ResearchSystem _research = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!; // imp

        private static readonly SoundPathSpecifier ApproveSound = new("/Audio/Effects/Cargo/ping.ogg"); // imp

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ResearchDiskComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<ResearchDiskComponent, MapInitEvent>(OnMapInit);
        }

        private void OnAfterInteract(EntityUid uid, ResearchDiskComponent component, AfterInteractEvent args)
        {
            if (!args.CanReach)
                return;


            // imp start
            if (HasComp<ResearchConsoleComponent>(args.Target))
            {
                if (!_research.TryGetClientServer(args.Target.Value, out var serverEnt, out var clientServer))
                    return;

                _research.ModifyServerPoints(serverEnt.Value, component.Points, clientServer);
                _popupSystem.PopupEntity(Loc.GetString("research-disk-inserted", ("points", component.Points)), args.Target.Value, args.User);
                _audio.PlayPvs(ApproveSound, uid);
                QueueDel(uid);
                args.Handled = true;
                return;
            }
            // imp end

            if (!TryComp<ResearchServerComponent>(args.Target, out var server))
                return;

            _research.ModifyServerPoints(args.Target.Value, component.Points, server);
            _popupSystem.PopupEntity(Loc.GetString("research-disk-inserted", ("points", component.Points)), args.Target.Value, args.User);
            QueueDel(uid);
            args.Handled = true;
        }

        private void OnMapInit(EntityUid uid, ResearchDiskComponent component, MapInitEvent args)
        {
            if (!component.UnlockAllTech)
                return;

            component.Points = _prototype.EnumeratePrototypes<TechnologyPrototype>()
                .Sum(tech => tech.Cost);
        }
    }
}
