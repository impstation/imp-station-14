using Content.Shared._Impstation.Body.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Containers;

namespace Content.Shared._Impstation.Body.Systems
{
    public sealed class NoseSystem : EntitySystem
    {
        [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;

        public const string DefaultSolutionName = "nose";

        public override void Initialize()
        {
            SubscribeLocalEvent<NoseComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        }

        private void OnEntRemoved(Entity<NoseComponent> ent, ref EntRemovedFromContainerMessage args)
        {
            if (ent.Comp.Solution is not { } solution || args.Entity != solution.Owner)
                return;

            ent.Comp.Solution = null;
        }

        public bool CanTransferSolution(
            EntityUid uid,
            Solution solution,
            NoseComponent? nose = null,
            SolutionContainerManagerComponent? solutions = null)
        {
            return Resolve(uid, ref nose, ref solutions, logMissing: false)
                && _solutionContainerSystem.ResolveSolution((uid, solutions), DefaultSolutionName, ref nose.Solution, out var noseSolution)
                && noseSolution.CanAddSolution(solution);
        }

        public bool TryTransferSolution(
            EntityUid uid,
            Solution solution,
            NoseComponent? nose = null,
            SolutionContainerManagerComponent? solutions = null)
        {
            if (!Resolve(uid, ref nose, ref solutions, logMissing: false)
                || !_solutionContainerSystem.ResolveSolution((uid, solutions), DefaultSolutionName, ref nose.Solution)
                || !CanTransferSolution(uid, solution, nose, solutions))
            {
                return false;
            }

            _solutionContainerSystem.TryAddSolution(nose.Solution.Value, solution);
            return true;
        }
    }
}
