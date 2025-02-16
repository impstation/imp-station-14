using System.ComponentModel.DataAnnotations;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Impstation.Power
{
    /// <summary>
    /// Converts spesos into heat and money.
    /// </summary>
    [RegisterComponent, AutoGenerateComponentPause]
    public sealed partial class ItemMinerComponent : Component
    {
        [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
        public TimeSpan NextSecond;

        /// <summary>
        /// The amount of energy produced each second when mining.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public float EnergyPerSecond = 10000;

        /// <summary>
        /// Charge threashold at which charge is converted into spesos.
        /// </summary>
        [DataField]
        public float ChargeThreashold = 1000000000;

        /// <summary>
        /// Id of the item created when the threashold is reached.
        /// </summary>
        [DataField(required: true)]
        public string ItemConvertion;

        /// <summary>
        /// Id of the item created when the threashold is reached after being emagged.
        /// If left empty, cannot be emagged.
        /// </summary>
        [DataField]
        public string EmagConvertion = string.Empty;

        /// <summary>
        /// Item created when the threashold is reached after being emagged.
        /// </summary>
        [DataField]
        public bool IsEmagged = false;
    }
}
