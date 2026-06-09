using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Content.Server.Power.Generation.Teg;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.Guidebook;

namespace Content.Server.Atmos.Piping.Unary.Components
{
    [RegisterComponent]
    [Access(typeof(GasOutletInjectorSystem), typeof(TegSystem))] // IMP ADD: access to TegSystem
    public sealed partial class GasOutletInjectorComponent : Component
    {

        [DataField] // IMP ADD: make accessible in YAML
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled = true;

        /// <summary>
        ///     Target volume to transfer. If <see cref="WideNet"/> is enabled, actual transfer rate will be much higher.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float TransferRate
        {
            get => _transferRate;
            set => _transferRate = Math.Clamp(value, 0f, MaxTransferRate);
        }

        private float _transferRate = 50;

        [DataField]
        public float MaxTransferRate = Atmospherics.MaxTransferRate;

        [DataField]
        [GuidebookData]
        public float MaxPressure = GasVolumePumpComponent.DefaultHigherThreshold;

        [DataField("inlet")]
        public string InletName = "pipe";
    }
}
