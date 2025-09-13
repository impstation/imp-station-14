using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Tourist.Components
{
    [RegisterComponent, NetworkedComponent, Access(typeof(SharedTouristCameraSystem))]
    public sealed partial class TouristCameraComponent : Component
    {

        [DataField("duration")]
        [ViewVariables(VVAccess.ReadWrite)]
        public int FlashDuration { get; set; } = 5000;

        [DataField("range")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float Range { get; set; } = 7f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("aoeFlashDuration")]
        public TimeSpan AoeFlashDuration = TimeSpan.FromSeconds(2);

        [DataField("slowTo")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float SlowTo { get; set; } = 0.5f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("sound")]
        public SoundSpecifier Sound { get; set; } = new SoundPathSpecifier("/Audio/Weapons/flash.ogg")
        {
            Params = AudioParams.Default.WithVolume(1f).WithMaxDistance(3f)
        };

        [DataField("doAfterDuration")]
        public float DoAfterDuration = 1f;

        public bool Flashing;

        [DataField]
        public float Probability = 1f;

    }

    [Serializable, NetSerializable]
    public enum TouristCameraVisuals : byte
    {
        BaseLayer,
        LightLayer,
        Flashing,
    }
}
