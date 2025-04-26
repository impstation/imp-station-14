using Content.Server.Emp;
using Content.Server._Impstation.Radio.Components;
using Content.Server.Radio.Components;
using Content.Shared.Interaction;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Server._Impstation.Radio;

public sealed class SelfHeadsetSystem : EntitySystem
{

    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SelfHeadsetComponent, EncryptionChannelsChangedEvent>(OnKeysChanged);
        SubscribeLocalEvent<SelfHeadsetComponent, EmpPulseEvent>(OnEmpPulse);

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
