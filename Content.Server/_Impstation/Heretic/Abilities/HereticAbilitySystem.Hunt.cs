using Content.Shared.Heretic;

namespace Content.Server.Heretic.Abilities;

public sealed partial class HereticAbilitySystem : EntitySystem
{
    private void SubscribeHunt()
    {
        SubscribeLocalEvent<HereticComponent, EventHereticPlaceWatchtower>(OnPlaceWatchtower);
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
    private void OnSerpentFocus(Entity<HereticComponent> ent, ref EventHereticSerpentFocus args)
    {
    }
    private void OnHuntAscend(Entity<HereticComponent> ent, ref EventHereticHuntAscend args)
    {
    }
}
