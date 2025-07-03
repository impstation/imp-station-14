using Content.Server.Pinpointer;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Access.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared.PDA;
using Robust.Shared.Containers;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed class SOSCartridgeSystem : EntitySystem
{
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

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

    private void OnActivated(Entity<SOSCartridgeComponent> ent, ref CartridgeActivatedEvent args) // imp - refactored to use Entity<T>
    {
        if (ent.Comp.CanCall)
        {
            //Get the PDA
            if (!TryComp<PdaComponent>(args.Loader, out var pda))
                return;

            //Get the id container
            if (_container.TryGetContainer(args.Loader, SOSCartridgeComponent.PDAIdContainer, out var idContainer))
            {
                SendMessage(ent, idContainer); // imp - changed all this shit

                ent.Comp.Timer = SOSCartridgeComponent.TimeOut;
            }
        }
    }

    // imp - condensed this down into a method to reduce repetition
    private void SendMessage(Entity<SOSCartridgeComponent> ent, BaseContainer idCards)
    {
        string? location = null;
        // get the location if we need the location
        if (ent.Comp.SendLocation)
        {
            var mapCoords = _xform.ToMapCoordinates(Transform(ent).Coordinates);
            if (_navMap.TryGetNearestBeacon(mapCoords, out var beacon, out _) && beacon?.Comp.Text is { } beaconText)
                location = beaconText;
        }
        // if location was assigned a value, send the location message. if not, send the default message.
        var msg = location != null ? Loc.GetString(ent.Comp.HelpMessageLocation, ("location", location)) : Loc.GetString(ent.Comp.HelpMessage);

        // send anonymously if there is no ID in the slot
        if (idCards.ContainedEntities.Count == 0)
            _radio.SendRadioMessage(ent, Loc.GetString(ent.Comp.DefaultName) + " " + msg, ent.Comp.HelpChannel, ent);
        // otherwise, send with the full name
        else
        {
            foreach (var idCard in idCards.ContainedEntities)
            {
                if (!TryComp<IdCardComponent>(idCard, out var idCardComp))
                    return;

                _radio.SendRadioMessage(ent, idCardComp.FullName + " " + msg, ent.Comp.HelpChannel, ent);
            }
        }
    }
}
