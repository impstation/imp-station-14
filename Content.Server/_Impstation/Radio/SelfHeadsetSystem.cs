using Content.Server._Impstation.Radio.Components;
using Content.Server.Radio.Components;
using Robust.Shared.Containers;
using Content.Shared.Inventory;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;

// from mq to asa: when ur done, make sure you delete the unused usings and organise ur usings in alphabetical order <3
// do this for component file & dependencies too

namespace Content.Server._Impstation.Radio;

public sealed class SelfHeadsetSystem : EntitySystem
{

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SelfHeadsetComponent, EncryptionChannelsChangedEvent>(OnKeysChanged);

    }


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
                EnsureComp<IntrinsicRadioTransmitterComponent>(uid).Channels = new(keyHolder.Channels);
        }
    }

}
