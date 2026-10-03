using Content.Server.Body.Systems;
using Content.Shared._Impstation.Heretic.Components;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent; // imp
using Content.Shared.Damage.Components; // imp
using Content.Shared.DoAfter;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Heretic;
using Content.Shared.Humanoid; // imp
using Content.Shared.Popups;
using Content.Shared.Speech.Muting;
using Content.Shared.Sprite; // imp
using Robust.Shared.Audio;

namespace Content.Server.Heretic.Abilities;

public sealed partial class HereticAbilitySystem : EntitySystem
{
    [Dependency] private SharedVisualBodySystem _visualBody = default!;
    [Dependency] private BloodstreamSystem _blood = default!;

    private void SubscribeFlesh()
    {
        SubscribeLocalEvent<HereticComponent, EventHereticFleshSurgery>(OnFleshSurgery);
        SubscribeLocalEvent<HereticComponent, EventHereticFleshSurgeryDoAfter>(OnFleshSurgeryDoAfter);
        SubscribeLocalEvent<HereticComponent, EventHereticFleshAscend>(OnFleshAscendPolymorph);
    }

    private void OnFleshSurgery(Entity<HereticComponent> ent, ref EventHereticFleshSurgery args)
    {
        if (!TryUseAbility(ent, args))
            return;

        if (HasComp<MinionComponent>(args.Target)
        || TryComp<HereticComponent>(args.Target, out var th) && th.MainPath == ent.Comp.MainPath)
        {
            var dargs = new DoAfterArgs(EntityManager, ent, 10f, new EventHereticFleshSurgeryDoAfter(), ent, args.Target)
            {
                BreakOnDamage = true,
                BreakOnMove = true,
                BreakOnHandChange = false,
                BreakOnDropItem = false,
            };
            _doafter.TryStartDoAfter(dargs);
            args.Handled = true;
            return;
        }

        // temporarily disable a random organ
        // the fucking goob coders were literally just deleting organ components. who thought that was okay
        // TODO: change this to actually remove/disable organs once newmed comes out next week
        if (TryComp<BodyComponent>(args.Target, out var body))
        {
            //i should really make these their own methods. but i dont want to
            switch (_random.Next(0, 3))
            {
                // case 0: barf
                case 0:
                    _popup.PopupEntity(Loc.GetString("heretic-fleshsurgery-barf"), args.Target, args.Target, PopupType.LargeCaution);
                    _vomit.Vomit(args.Target, -1000, -1000); // i frew up
                    break;

                // case 1: blind (fixable!)
                case 1:
                    if (!TryComp<BlindableComponent>(args.Target, out var blindable) || blindable.IsBlind)
                        return;

                    _popup.PopupEntity(Loc.GetString("heretic-fleshsurgery-eyes"), args.Target, args.Target, PopupType.LargeCaution);
                    _blindable.AdjustEyeDamage((args.Target, blindable), 7); //same as rawdogging a welder 7 times. fixable but definitely a pain
                    break;

                // case 2: mute for 2.5 minutes
                case 2:
                    _popup.PopupEntity(Loc.GetString("heretic-fleshsurgery-mute"), args.Target, args.Target, PopupType.LargeCaution);
                    _statusEffect.TryAddStatusEffect<MutedComponent>(args.Target, "Muted", TimeSpan.FromSeconds(150), false);
                    break;

                default:
                    break;
            }
        }

        args.Handled = true;
    }
    private void OnFleshSurgeryDoAfter(Entity<HereticComponent> ent, ref EventHereticFleshSurgeryDoAfter args)
    {
        if (args.Cancelled)
            return;

        if (args.Target == null) // shouldn't really happen. just in case
            return;

        if (!TryComp<DamageableComponent>(args.Target, out var dmg))
            return;

        // heal teammates, mostly ghouls
        _dmg.SetAllDamage(((EntityUid)args.Target, dmg), 0);
        args.Handled = true;
    }
    private void OnFleshAscendPolymorph(Entity<HereticComponent> ent, ref EventHereticFleshAscend args)
    {
        if (!TryUseAbility(ent, args))
            return;

        var urist = _poly.PolymorphEntity(ent, "EldritchHorror");
        if (urist == null)
            return;

        // This is some hardcoded jank and this should absolutely be raising an event for the color data instead of whatever it's currently doing.
        if (GrabHumanoidColors(ent) is { } colors) // begin imp
        {
            if (TryComp<RandomSpriteComponent>(urist, out var randomSprite)) // we have to do this using RandomSpriteComponent, otherwise I'd be making a whole species prototype just for this.
            {
                foreach (var entry in randomSprite.Selected)
                {
                    var state = randomSprite.Selected[entry.Key];
                    switch (entry.Key)
                    {
                        case "fleshMap":
                            state.Color = colors.SkinColor;
                            break;
                        case "eyesMap":
                            state.Color = colors.EyeColor;
                            break;
                        case "bloodMap":
                            state.Color = colors.BloodColor;
                            break;
                    }
                    randomSprite.Selected[entry.Key] = state;
                }
                Dirty(urist.Value, randomSprite);
            }
        } // end imp

        _aud.PlayPvs(new SoundPathSpecifier("/Audio/Animals/space_dragon_roar.ogg"), (EntityUid)urist, AudioParams.Default.AddVolume(2f));

        args.Handled = true;
    }

    private (Color SkinColor, Color EyeColor, Color BloodColor)? GrabHumanoidColors(EntityUid entity) // imp
    {
        if (!TryComp<BloodstreamComponent>(entity, out var bloodstream) || !_visualBody.TryGatherMarkingsData(entity, null, out var profiles, out _, out _))
            return null; // if (for some reason - like perhaps admin intervention) a non-humanoid or someone with no bloodstream ascends, we don't want to try to modify the colors.

        var bloodColor = _blood.GetBloodReferenceColor(bloodstream);
        if (profiles.Count == 0)
            return (Color.White, Color.White, bloodColor);

        var skinColor = Color.Black;
        var eyeColor = Color.Black;
        foreach (var (_, profile) in profiles) // TODO: Make an engine PR to reduce the boilerplate of this shit by a morbillion.
        {
            skinColor.R += profile.SkinColor.R;
            skinColor.G += profile.SkinColor.G;
            skinColor.B += profile.SkinColor.B;
            eyeColor.R += profile.EyeColor.R;
            eyeColor.G += profile.EyeColor.G;
            eyeColor.B += profile.EyeColor.B;
        }

        skinColor.R /= profiles.Count;
        skinColor.G /= profiles.Count;
        skinColor.B /= profiles.Count;
        eyeColor.R /= profiles.Count;
        eyeColor.G /= profiles.Count;
        eyeColor.B /= profiles.Count;

        return (skinColor, eyeColor, bloodColor);
    }
}
