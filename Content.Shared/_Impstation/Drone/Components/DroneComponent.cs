using Content.Shared.Whitelist;
using Content.Shared.Alert;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Drone.Components
{
    [RegisterComponent]
    [NetworkedComponent, AutoGenerateComponentPause, AutoGenerateComponentState]
    public sealed partial class DroneComponent : Component
    {
        public float InteractionBlockRange = 1.5f; /// imp. original value was 2.15, changed because it was annoying. this also does not actually block interactions anymore.

        // imp. delay before posting another proximity alert
        public TimeSpan ProximityDelay = TimeSpan.FromMilliseconds(2000);

        [AutoPausedField]
        public TimeSpan NextProximityAlert = new();

        public EntityUid NearestEnt = default!;

        [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
        public EntityWhitelist? Whitelist;

        [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
        public EntityWhitelist? Blacklist;

        [DataField]
        public ProtoId<AlertPrototype> BatteryAlert = "BorgBattery";

        [DataField]
        public ProtoId<AlertPrototype> NoBatteryAlert = "BorgBatteryNone";

        [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
        [AutoNetworkedField, AutoPausedField]
        public TimeSpan NextBatteryUpdate = TimeSpan.Zero;
    }
}
