using Content.Server.Emp;
using Content.Server._Impstation.Radio.Components;
using Content.Server.Radio.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Content.Shared.Interaction;

namespace Content.Server._Impstation.Radio;

public sealed class SelfHeadsetSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SelfHeadsetComponent, EncryptionChannelsChangedEvent>(OnKeysChanged);
        SubscribeLocalEvent<SelfHeadsetComponent, EmpPulseEvent>(OnEmpPulse);
        SubscribeLocalEvent<SelfHeadsetComponent, InteractUsingEvent>(OnInteractUsing);
    }

    /// <summary>
    /// Used to give an entity access to radio channels when an Encryption Key is inserted, without the need for a headset.
    /// </summary>

    private void OnKeysChanged(EntityUid uid, SelfHeadsetComponent component, ref EncryptionChannelsChangedEvent args)
    {
        UpdateRadioChannels(uid, component, args.Component);
    }

    private void UpdateRadioChannels(EntityUid uid, SelfHeadsetComponent component, EncryptionKeyHolderComponent? keyHolder = null)
    {
        {
            if (!Resolve(uid, ref keyHolder))
                return;

            if (keyHolder.Channels.Count == 0)
                RemCompDeferred<ActiveRadioComponent>(uid);
            else
                EnsureComp<ActiveRadioComponent>(uid).Channels = new(keyHolder.Channels);
        }

        {
            if (!Resolve(uid, ref keyHolder))
                return;

            if (keyHolder.Channels.Count == 0)
                RemCompDeferred<IntrinsicRadioReceiverComponent>(uid);
            else
                EnsureComp<IntrinsicRadioReceiverComponent>(uid);
        }

        {
            if (!Resolve(uid, ref keyHolder))
                return;

            if (keyHolder.Channels.Count == 0)
                RemCompDeferred<IntrinsicRadioTransmitterComponent>(uid);
            else
                EnsureComp<IntrinsicRadioTransmitterComponent>(uid).Channels = new(keyHolder.Channels);
        }
    }

    private void OnInteractUsing(EntityUid uid, SelfHeadsetComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (HasComp<SelfHeadsetComponent>(args.Used))
        {
            args.Handled = true;
            TryInsertKey(uid, component, args);
        }
    }

    private void TryInsertKey(EntityUid uid, SelfHeadsetComponent component, InteractUsingEvent args)
    {
        if (_container.Insert(args.Used, component.KeyContainer))
        {
            _audio.PlayPredicted(component.KeyInsertionSound, uid, uid);
            args.Handled = true;
            return;
        }
    }

    /// <summary>
    /// Disables radio when hit by an EMP.
    /// </summary>
    private void OnEmpPulse(EntityUid uid, SelfHeadsetComponent component, ref EmpPulseEvent args)
    {
        {
            args.Affected = true;
            args.Disabled = true;
        }
    }

}
