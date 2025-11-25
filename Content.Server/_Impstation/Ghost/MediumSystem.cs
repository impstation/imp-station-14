using Content.Shared.Actions;
using Content.Shared.Eye;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Timing;

namespace Content.Server._Impstation.Ghost
{
    public sealed class MediumSystem : EntitySystem
    {
        [Dependency] private readonly SharedEyeSystem _eye = default!;
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;


        public override void Initialize()
        {
            SubscribeLocalEvent<MediumComponent, ComponentStartup>(OnMediumStartup);
            SubscribeLocalEvent<MediumComponent, ComponentRemove>(OnMediumRemove);
            SubscribeLocalEvent<MediumComponent, MapInitEvent>(OnMapInitMedium);
            SubscribeLocalEvent<MediumComponent, GetVisMaskEvent>(OnMediumVis);

            SubscribeLocalEvent<MediumStatusEffectComponent, StatusEffectAppliedEvent>(OnMediumStatusApplied);
            SubscribeLocalEvent<MediumStatusEffectComponent, StatusEffectRemovedEvent>(OnMediumStatusRemoved);
        }

        // serverside methods for the 'medium' reagent effects. lets affected entities see ghosts.
        private void OnMediumVis(Entity<MediumComponent> ent, ref GetVisMaskEvent args)
        {
            // If component not deleting they can see ghosts.
            if (ent.Comp.LifeStage <= ComponentLifeStage.Running)
            {
                args.VisibilityMask |= (int)VisibilityFlags.Ghost;
            }
        }

        private void OnMediumStartup(EntityUid uid, MediumComponent component, ComponentStartup args)
        {
            _eye.RefreshVisibilityMask(uid);
        }

        private void OnMediumRemove(EntityUid uid, MediumComponent component, ComponentRemove args)
        {
            _eye.RefreshVisibilityMask(uid);
            _actions.RemoveAction(uid, component.ToggleGhostsMediumActionEntity);
        }

        private void OnMapInitMedium(EntityUid uid, MediumComponent component, MapInitEvent args)
        {
            _actions.AddAction(uid, ref component.ToggleGhostsMediumActionEntity, component.ToggleGhostsMediumAction);
        }

        private void OnMediumStatusApplied(EntityUid uid, MediumStatusEffectComponent component, StatusEffectAppliedEvent args)
        {
            if (_gameTiming.ApplyingState)
                return;

            EnsureComp<MediumComponent>(args.Target);
        }

        private void OnMediumStatusRemoved(EntityUid uid, MediumStatusEffectComponent component, StatusEffectRemovedEvent args)
        {
            RemComp<MediumComponent>(args.Target);
        }
    }
}
