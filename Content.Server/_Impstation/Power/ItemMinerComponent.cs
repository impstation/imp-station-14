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
        /// <summary>
        /// Charge threashold at which charge is converted into spesos.
        /// </summary>
        [DataField]
        public float ChargeThreashold = 10000000;

        /// <summary>
        /// Item created when the threashold is reached.
        /// </summary>
        [DataField(required: true)]
        public string ItemConvertion;

        /// <summary>
        /// The amount of energy produced each second when mining.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public float EnergyPerSecond = 1000;

        [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
        public TimeSpan NextSecond;
    }
}
