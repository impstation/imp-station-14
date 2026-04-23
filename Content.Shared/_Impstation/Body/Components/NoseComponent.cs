using Content.Shared._Impstation.Body.Systems;
using Content.Shared.Chemistry.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Body.Components
{
    [RegisterComponent, NetworkedComponent, Access(typeof(NoseSystem))]
    public sealed partial class NoseComponent : Component
    {
        /// <summary>
        /// The solution inside this nose.
        /// </summary>
        [ViewVariables]
        public Entity<SolutionComponent>? Solution;
    }
}
