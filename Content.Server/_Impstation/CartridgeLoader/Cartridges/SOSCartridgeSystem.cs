using Content.Server.Radio.EntitySystems;
using Content.Shared.Access.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared.PDA;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Random;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed class SOSCartridgeSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SOSCartridgeComponent, CartridgeActivatedEvent>(OnActivated);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SOSCartridgeComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Timer > 0)
            {
                comp.Timer -= frameTime;
            }
        }
    }

    private void OnActivated(EntityUid uid, SOSCartridgeComponent component, CartridgeActivatedEvent args)
    {
        if (component.CanCall)
        {
            //Get the PDA
            if (!TryComp<PdaComponent>(args.Loader, out var pda))
                return;

            //Get the id container
            if (_container.TryGetContainer(args.Loader, SOSCartridgeComponent.PDAIdContainer, out var idContainer))
            {
                //If theres nothing in id slot, send message anonymously
                if (idContainer.ContainedEntities.Count == 0)
                {
                    _radio.SendRadioMessage(uid, component.LocalizedDefaultName + " " + component.LocalizedHelpMessage, component.HelpChannel, uid);
                }
                else
                {
                    //Otherwise, send a message with the full name of every id in there
                    foreach (var idCard in idContainer.ContainedEntities)
                    {
                        if (!TryComp<IdCardComponent>(idCard, out var idCardComp))
                            return;

                        _radio.SendRadioMessage(uid, idCardComp.FullName + " " + component.LocalizedHelpMessage, component.HelpChannel, uid);
                    }
                }
                // Sound effect that is heard nearby
                var sound = _random.Prob(component.TomSoundChance) ? component.TomActivationSound : component.ActivationSound;
                _audio.PlayPvs(sound, args.Loader);
                component.Timer = SOSCartridgeComponent.TimeOut;
            }
        }
    }
}
