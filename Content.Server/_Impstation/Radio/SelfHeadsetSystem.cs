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
        SubscribeLocalEvent<SelfHeadsetComponent, EntGotInsertedIntoContainerMessage>(OnAdd);
        SubscribeLocalEvent<SelfHeadsetComponent, EncryptionChannelsChangedEvent>(OnKeysChanged);
        SubscribeLocalEvent<SelfHeadsetComponent, EntGotRemovedFromContainerMessage>(OnRemove);
    }

    /// <summary>
    /// If an encryption key is added, installs the necessary intrinsic radio components
    /// </summary>
    private void OnAdd(Entity<SelfHeadsetComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {

        var activeRadio = EnsureComp<ActiveRadioComponent>(args.Container.Owner);
        foreach (var channel in ent.Comp.RadioChannels)
        {
            if (activeRadio.Channels.Add(channel))
                ent.Comp.ActiveAddedChannels.Add(channel);
        }

        EnsureComp<IntrinsicRadioReceiverComponent>(args.Container.Owner);

        var intrinsicRadioTransmitter = EnsureComp<IntrinsicRadioTransmitterComponent>(args.Container.Owner);
        foreach (var channel in ent.Comp.RadioChannels)
        {
            if (intrinsicRadioTransmitter.Channels.Add(channel))
                ent.Comp.TransmitterAddedChannels.Add(channel);
        }
    }

    private void OnKeysChanged(EntityUid uid, SelfHeadsetComponent component, EncryptionChannelsChangedEvent args)
    {
        UpdateRadioChannels(uid, component, args.Component);
    }

    private void UpdateRadioChannels(EntityUid uid, SelfHeadsetComponent component, EncryptionKeyHolderComponent? keyHolder = null)
    {

        if (!Resolve(uid, ref keyHolder))
            return;

        if (keyHolder.Channels.Count == 0)
            RemComp<ActiveRadioComponent>(uid);
        else
            EnsureComp<ActiveRadioComponent>(uid).Channels = new(keyHolder.Channels);
    }


    /// <summary>
    /// Removes intrinsic radio components once the encryption key is removed
    /// </summary>
    private void OnRemove(Entity<SelfHeadsetComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (TryComp<ActiveRadioComponent>(args.Container.Owner, out var activeRadioComponent))
        {
            foreach (var channel in ent.Comp.ActiveAddedChannels)
            {
                activeRadioComponent.Channels.Remove(channel);
            }
            ent.Comp.ActiveAddedChannels.Clear();

            if (activeRadioComponent.Channels.Count == 0)
            {
                RemCompDeferred<ActiveRadioComponent>(args.Container.Owner);
            }
        }

        if (!TryComp<IntrinsicRadioTransmitterComponent>(args.Container.Owner, out var radioTransmitterComponent))
            return;

        foreach (var channel in ent.Comp.TransmitterAddedChannels)
        {
            radioTransmitterComponent.Channels.Remove(channel);
        }
        ent.Comp.TransmitterAddedChannels.Clear();

        if (radioTransmitterComponent.Channels.Count == 0 || activeRadioComponent?.Channels.Count == 0)
        {
            RemCompDeferred<IntrinsicRadioTransmitterComponent>(args.Container.Owner);
        }
    }
}
