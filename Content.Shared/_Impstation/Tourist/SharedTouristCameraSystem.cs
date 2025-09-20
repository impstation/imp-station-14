using Content.Shared.Flash.Components;
using Content.Shared.StatusEffect;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Tourist;

public abstract partial class SharedTouristCameraSystem : EntitySystem
{
    public ProtoId<StatusEffectPrototype> FlashedKey = "Flashed";

    public virtual void FlashArea(Entity<FlashComponent?> source, EntityUid? user, float range, float duration, float slowTo = 0.8f, bool displayPopup = false, float probability = 1f, SoundSpecifier? sound = null)
    {
    }

    /// <summary>
    ///     Called when the camera is used to force a doafter
    /// </summary>
    [Serializable, NetSerializable]
    public sealed partial class TouristCameraDoAfterEvent : SimpleDoAfterEvent
    {
    }
}
