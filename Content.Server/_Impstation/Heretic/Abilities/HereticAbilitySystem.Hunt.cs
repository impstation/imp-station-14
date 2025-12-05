using Content.Shared._Impstation.Weapons.Redirect;
using Content.Shared.Heretic;
using Content.Shared.Heretic.Components;
using Robust.Shared.Audio;

namespace Content.Server.Heretic.Abilities;

public sealed partial class HereticAbilitySystem : EntitySystem
{
    public SoundSpecifier SummonTowerSound = new SoundPathSpecifier("/Audio/Magic/Ethereal_Exit.ogg");
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
        Spawn("HereticWatchtower", _transform.GetMapCoordinates(ent, xform: xform));
        _audio.PlayPvs(SummonTowerSound, ent, AudioParams.Default.WithVolume(-3f));
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
        EnsureComp<ProjectileRedirectorComponent>(ent);
    }
}
