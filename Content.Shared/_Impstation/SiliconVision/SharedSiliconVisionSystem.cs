using Content.Shared.Actions;
using Content.Shared.Eye;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Timing;

namespace Content.Shared._Impstation.SiliconVision
{
    public sealed class SharedSiliconVisionSystem : EntitySystem
    {
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly SharedEyeSystem _eye = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;


        public override void Initialize()
        {
            SubscribeLocalEvent<SiliconVisionComponent, ComponentStartup>(OnSiliconVisionStartup);
            SubscribeLocalEvent<SiliconVisionComponent, ComponentRemove>(OnSiliconVisionRemove);
            SubscribeLocalEvent<SiliconVisionComponent, GetVisMaskEvent>(OnSiliconVisionVis);

        }

         /// <summary>
        /// Updates VisMasks to let player see entities with Silicon VisibilityFlags when component is running on an ent.
        /// </summary>
        private void OnSiliconVisionVis(Entity<SiliconVisionComponent> ent, ref GetVisMaskEvent args)
        {
            if (ent.Comp.LifeStage <= ComponentLifeStage.Running)
            {
                args.VisibilityMask |= (int)VisibilityFlags.Silicon;
            }
        }

        /// <summary>
        /// Refreshes VisMasks on component init
        /// </summary>
        private void OnSiliconVisionStartup(Entity<SiliconVisionComponent> ent, ref ComponentStartup args)
        {
            _eye.RefreshVisibilityMask(ent.Owner);
        }

        /// <summary>
        /// Refreshes VisMasks on component remove
        /// </summary>
        private void OnSiliconVisionRemove(Entity<SiliconVisionComponent> ent, ref ComponentRemove args)
        {
            _eye.RefreshVisibilityMask(ent.Owner);
        }
    }
}
