using Content.Shared.Actions;
using Content.Shared.Eye;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Timing;

namespace Content.Server._Impstation.Ghost
{
    public sealed class MediumSystem : EntitySystem
    {
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly SharedEyeSystem _eye = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;


        public override void Initialize()
        {
            SubscribeLocalEvent<MediumComponent, ComponentStartup>(OnMediumStartup);
            SubscribeLocalEvent<MediumComponent, ComponentRemove>(OnMediumRemove);
            SubscribeLocalEvent<MediumComponent, GetVisMaskEvent>(OnMediumVis);

            SubscribeLocalEvent<MediumStatusEffectComponent, StatusEffectAppliedEvent>(OnMediumStatusApplied);
            SubscribeLocalEvent<MediumStatusEffectComponent, StatusEffectRemovedEvent>(OnMediumStatusRemoved);
        }

        /// <summary>
        /// Updates VisMasks to let player see entities with Ghost VisibilityFlags when component is running on an ent.
        /// </summary>
        private void OnMediumVis(Entity<MediumComponent> ent, ref GetVisMaskEvent args)
        {
            if (ent.Comp.LifeStage <= ComponentLifeStage.Running)
            {
                args.VisibilityMask |= (int)VisibilityFlags.Ghost;
            }
        }

        /// <summary>
        /// Refreshes VisMasks on component init and adds the ToggleGhosts action
        /// </summary>
        private void OnMediumStartup(Entity<MediumComponent> ent, ref ComponentStartup args)
        {
            _eye.RefreshVisibilityMask(ent.Owner);
            _actions.AddAction(ent, ref ent.Comp.ToggleGhostsMediumActionEntity, ent.Comp.ToggleGhostsMediumAction);
        }

        /// <summary>
        /// Refreshes VisMasks on component remove and gets rid of the ToggleGhosts action
        /// </summary>
        private void OnMediumRemove(Entity<MediumComponent> ent, ref ComponentRemove args)
        {
            _eye.RefreshVisibilityMask(ent.Owner);
            _actions.RemoveAction(ent.Owner, ent.Comp.ToggleGhostsMediumActionEntity);
        }

        /// <summary>
        /// Adds MediumComp when the 'medium afflicted' status effect is applied to an ent.
        /// </summary>
        private void OnMediumStatusApplied(Entity<MediumStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
        {
            if (_gameTiming.ApplyingState)
                return;

            EnsureComp<MediumComponent>(args.Target);
        }

        /// <summary>
        /// Removes MediumComp when the 'medium afflicted' status effect is taken away from an ent.
        /// (usually by running out of time)
        /// </summary>
        private void OnMediumStatusRemoved(Entity<MediumStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
        {
            RemComp<MediumComponent>(args.Target);
        }
    }
}
