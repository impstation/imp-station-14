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
    }
    private void OnSerpentFocus(Entity<HereticComponent> ent, ref EventHereticSerpentFocus args)
    {
    }
    private void OnHuntAscend(Entity<HereticComponent> ent, ref EventHereticHuntAscend args)
    {
    }
}
