using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.GameTicking.Rules.VariationPass.Components
{
    [RegisterComponent]
    public sealed partial class ModifyComponentsVariationPassComponent : Component
    {
        /// <summary>
        /// The list of components that will be added.
        /// </summary>
        [DataField]
        public ComponentRegistry AddedComponents = new();

        /// <summary>
        /// Should components that already exist on the entity be overwritten?
        /// </summary>
        [DataField]
        public bool RemoveExisting = false;

        /// <summary>
        /// The list of components that will be removed.
        /// </summary>
        /// TODO: Using a ComponentRegistry for this is cursed because it stores all the datafields along with it
        /// but ComponentNameSerializer will complain if you have components that are not in shared.
        /// From RemoveComponentsOnTriggerComponent.
        [DataField]
        public ComponentRegistry RemovedComponents = new();

        [DataField]
        public MinMax MinMaxComponentRecievers = new(1, 1);
    }
}
