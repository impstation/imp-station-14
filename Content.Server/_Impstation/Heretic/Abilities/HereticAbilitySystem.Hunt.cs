using Content.Shared._EE.Overlays.Switchable;
using Content.Shared.Heretic;
using Content.Shared.Heretic.Components;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using System;
using NetCord;

namespace Content.Server.Heretic.Abilities;

public sealed partial class HereticAbilitySystem : EntitySystem
{
    private void SubscribeHunt()
    {
        SubscribeLocalEvent<HereticComponent, EventHereticPlaceWatchtower>(OnPlaceWatchtower);
        SubscribeLocalEvent<HereticComponent, EventHereticTeachSerpentFocus>(OnTeachSerpentFocus);
        SubscribeLocalEvent<HereticComponent, EventHereticSerpentFocus>(OnSerpentFocus);
        SubscribeLocalEvent<HereticComponent, EventHereticHuntAscend>(OnHuntAscend);
    }

    private void OnPlaceWatchtower(Entity<HereticComponent> ent, ref EventHereticPlaceWatchtower args)
    {
        if (!TryUseAbility(ent, args))
            return;

        var xform = Transform(ent);
        Spawn("hereticWatchtower", _transform.GetMapCoordinates(ent, xform: xform));
        args.Handled = true;
    }
    private void OnTeachSerpentFocus(Entity<HereticComponent> ent, ref EventHereticTeachSerpentFocus args) //fuck you fuck you fuck you fuck you fuck you fuck you fuck you fuck you
    {
        EnsureComp<SerpentFocusComponent>(ent);
        args.Handled = true;
    }
    private void OnSerpentFocus(Entity<HereticComponent> ent, ref EventHereticSerpentFocus args)
    {
        if (!TryUseAbility(ent, args))
            return;

        var ev = new ToggleSerpentFocusEvent();
        RaiseLocalEvent(ent, ev);

        args.Handled = true;
    }
    private void OnHuntAscend(Entity<HereticComponent> ent, ref EventHereticHuntAscend args)
    {
    }
}
