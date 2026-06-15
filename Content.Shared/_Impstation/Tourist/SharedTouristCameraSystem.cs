using Content.Shared.Light;
using Content.Shared.Flash.Components;
using Content.Shared.StatusEffect;
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Events;
using Content.Shared._Impstation.Tourist.Components;
using Robust.Shared.Serialization;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Tourist;

public abstract partial class SharedTouristCameraSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TouristCameraComponent, UseInHandEvent>(OnCameraUseInHand);
    }
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

    private void OnCameraUseInHand(EntityUid uid, TouristCameraComponent comp, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, comp.DoAfterDuration, new TouristCameraDoAfterEvent(), uid, target: uid)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        }
}
