using Content.Shared.Movement.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Carrying
{
    public sealed class CarryingSlowdownSystem : EntitySystem
    {
        [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CarryingSlowdownComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<CarryingSlowdownComponent, ComponentHandleState>(OnHandleState);
            SubscribeLocalEvent<CarryingSlowdownComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMoveSpeed);
        }

        public void SetModifier(Entity<CarryingSlowdownComponent?> ent, float walkSpeedModifier, float sprintSpeedModifier)
        {
            if (!Resolve(ent, ref ent.Comp))
                return;

            ent.Comp.WalkModifier = walkSpeedModifier;
            ent.Comp.SprintModifier = sprintSpeedModifier;
            _movementSpeed.RefreshMovementSpeedModifiers(ent);
        }
        private void OnGetState(Entity<CarryingSlowdownComponent> ent, ref ComponentGetState args)
        {
            args.State = new CarryingSlowdownComponentState(ent.Comp.WalkModifier, ent.Comp.SprintModifier);
        }

        private void OnHandleState(Entity<CarryingSlowdownComponent> ent, ref ComponentHandleState args)
        {
            if (args.Current is not CarryingSlowdownComponentState state)
                return;

            ent.Comp.WalkModifier = state.WalkModifier;
            ent.Comp.SprintModifier = state.SprintModifier;
            _movementSpeed.RefreshMovementSpeedModifiers(ent);
        }
        private void OnRefreshMoveSpeed(Entity<CarryingSlowdownComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
        {
            args.ModifySpeed(ent.Comp.WalkModifier, ent.Comp.SprintModifier);
        }
    }
}
