using Content.Server.GameTicking;
using Content.Shared.Eye;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;
using Content.Shared._Impstation.SiliconVision; //imp

namespace Content.Server._Impstation.SiliconVision
{
    public sealed class SiliconGhostSystem : SharedSiliconGhostSystem
    {
        [Dependency] private readonly SharedEyeSystem _eye = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly VisibilitySystem _visibilitySystem = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;

        private EntityQuery<SiliconGhostComponent> _siliconghostQuery;

        public override void Initialize()
        {
            base.Initialize();

            _siliconghostQuery = GetEntityQuery<SiliconGhostComponent>();

            SubscribeLocalEvent<SiliconGhostComponent, ComponentStartup>(OnSiliconGhostStartup);
            SubscribeLocalEvent<SiliconGhostComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<SiliconGhostComponent, ComponentShutdown>(OnSiliconGhostShutdown);
        }

        private void OnMapInit(EntityUid uid, SiliconGhostComponent component, MapInitEvent args)
        {
            _eye.RefreshVisibilityMask(uid);
        }

        private void OnSiliconGhostStartup(EntityUid uid, SiliconGhostComponent component, ComponentStartup args)
        {
            // Allow this entity to be seen by silicons.
            var visibility = EnsureComp<VisibilityComponent>(uid);

            if (_gameTicker.RunLevel != GameRunLevel.PostRound)
            {
                _visibilitySystem.AddLayer((uid, visibility), (int) VisibilityFlags.Silicon, false);
                _visibilitySystem.RemoveLayer((uid, visibility), (int) VisibilityFlags.Normal, false);
                _visibilitySystem.RefreshVisibility(uid, visibilityComponent: visibility);
            }

            _eye.RefreshVisibilityMask(uid);
        }

        private void OnSiliconGhostShutdown(EntityUid uid, SiliconGhostComponent component, ComponentShutdown args)
        {
            // Perf: If the entity is deleting itself, no reason to change these back.
            if (Terminating(uid))
                return;

            // Entity can't be seen by silicons anymore.
            if (TryComp(uid, out VisibilityComponent? visibility))
            {
                _visibilitySystem.RemoveLayer((uid, visibility), (int) VisibilityFlags.Silicon, false);
                _visibilitySystem.AddLayer((uid, visibility), (int) VisibilityFlags.Normal, false);
                _visibilitySystem.RefreshVisibility(uid, visibilityComponent: visibility);
            }

            // Entity can't see silicon ghosts anymore.
            _eye.RefreshVisibilityMask(uid);
        }
    }
}
