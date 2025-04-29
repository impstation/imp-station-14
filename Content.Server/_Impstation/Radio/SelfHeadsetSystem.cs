using Content.Server.Emp;
using Content.Server._Impstation.Radio.Components;
using Content.Server.Radio.Components;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;

namespace Content.Server._Impstation.Radio;

public sealed class SelfHeadsetSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SelfHeadsetComponent, EncryptionChannelsChangedEvent>(OnKeysChanged);
        SubscribeLocalEvent<SelfHeadsetComponent, EmpPulseEvent>(OnEmpPulse);
        SubscribeLocalEvent<SelfHeadsetComponent, ComponentStartup>(OnAdd);

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

    /// <summary>
    /// Ensures entities with SelfHeadset are capable of receiving encryption keys if not otherwise specified.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <param name="args"></param>
    private void OnAdd(EntityUid uid, SelfHeadsetComponent component, ref ComponentStartup args)
    {
        EnsureComp<EncryptionKeyHolderComponent>(uid);
    }
}