using Content.Server._Goobstation.Heretic.Components;
using System.Diagnostics.CodeAnalysis;
using Content.Server.Chat.Systems;
using Content.Server.Hands.Systems;
using Content.Server.Heretic.Components;
using Content.Server.Speech.EntitySystems;
using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Heretic;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Mobs.Components;
using Content.Shared.NPC.Prototypes;
using Content.Shared.RetractableItemAction;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Speech.Muting;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.Heretic.EntitySystems;

public sealed partial class MansusGraspSystem : EntitySystem
{
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly RatvarianLanguageSystem _language = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedDoorSystem _door = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly TemperatureSystem _temperature = default!;
    [Dependency] private readonly MinionSystem _minion = default!;
    [Dependency] private readonly HandsSystem _hands = default!;

    private readonly ProtoId<NpcFactionPrototype> _hereticFaction = "Heretic";

    public void ApplyGraspEffect(EntityUid performer, EntityUid target, string path)
    {
        switch (path)
        {
            case "Ash":
                var timeSpan = TimeSpan.FromSeconds(5f);
                _statusEffect.TryAddStatusEffect(target, TemporaryBlindnessSystem.BlindingStatusEffect, timeSpan, false, TemporaryBlindnessSystem.BlindingStatusEffect);
                break;

            case "Blade":
                // blade is basically an upgrade to the current grasp
                _stamina.TakeStaminaDamage(target, 100f);
                break;

            case "Lock":
                if (!TryComp<DoorComponent>(target, out var door))
                    break;

                if (TryComp<DoorBoltComponent>(target, out var doorBolt))
                    _door.SetBoltsDown((target, doorBolt), false);

                _door.StartOpening(target, door);
                _audio.PlayPvs(new SoundPathSpecifier("/Audio/_Goobstation/Heretic/hereticknock.ogg"), target);
                break;

            case "Flesh":
                if (TryComp<MobStateComponent>(target, out var mobState)
                    && mobState.CurrentState == Shared.Mobs.MobState.Dead
                    && !TryComp<HellVictimComponent>(target, out _))
                {
                    var minion = EnsureComp<MinionComponent>(target);
                    EnsureComp<GhoulComponent>(target);
                    minion.BoundOwner = performer;
                    minion.FactionsToAdd.Add(_hereticFaction);
                    _minion.ConvertEntityToMinion((target, minion), true, true, true);
                }
                break;

            case "Rust":
                if (!TryComp<DamageableComponent>(target, out var dmg))
                    break;
                // hopefully damage only walls and cyborgs
                if (HasComp<BorgChassisComponent>(target) || !HasComp<StatusEffectsComponent>(target))
                    _damage.SetAllDamage(target, dmg, 50f);
                break;

            case "Void":
                if (TryComp<TemperatureComponent>(target, out var temp))
                    _temperature.ForceChangeTemperature(target, temp.CurrentTemperature - 20f, temp);
                _statusEffect.TryAddStatusEffect<MutedComponent>(target, "Muted", TimeSpan.FromSeconds(8), false);
                break;

            default:
                return;
        }
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MansusGraspComponent, AfterInteractEvent>(OnAfterInteract);

        SubscribeLocalEvent<TagComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<HereticComponent, DrawRitualRuneDoAfterEvent>(OnRitualRuneDoAfter);
        SubscribeLocalEvent<MansusGraspComponent, UseInHandEvent>(OnUseInHand);
    }

    public bool MansusGraspActive(EntityUid heretic)
    {
        foreach (var hand in _hands.EnumerateHands(heretic))
        {
            if (!_hands.TryGetHeldItem(heretic, hand, out var heldEntity) ||
                !TryComp<MetaDataComponent>(heldEntity, out var metadata))
                continue;

            if (metadata.EntityPrototype?.ID == "TouchSpellMansus")
            {
                return true;
            }
        }
        return false;
    }
    private void OnAfterInteract(Entity<MansusGraspComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (args.Target == null || args.Target == args.User)
            return;

        if (!TryComp<HereticComponent>(args.User, out var hereticComp))
        {
            QueueDel(ent);
            return;
        }

        var target = (EntityUid)args.Target;

        if (TryComp<HereticComponent>(args.Target, out var heretic) && heretic.MainPath == ent.Comp.Path)
            return;

        args.Handled = true;

        if (HasComp<StatusEffectsComponent>(target))
        {
            _chat.TrySendInGameICMessage(args.User, Loc.GetString("heretic-speech-mansusgrasp"), InGameICChatType.Speak, false);
            _audio.PlayPvs(new SoundPathSpecifier("/Audio/Items/welder.ogg"), target);
            _stun.TryKnockdown(target, TimeSpan.FromSeconds(3f), true);
            _stamina.TakeStaminaDamage(target, 80f);
            _language.DoRatvarian(target, TimeSpan.FromSeconds(10f), true);
        }

        // upgraded grasp
        if (hereticComp.MainPath != null)
        {
            if (hereticComp.Power >= 2)
                ApplyGraspEffect(args.User, target, hereticComp.MainPath!);

            if (hereticComp.Power >= 4 && HasComp<StatusEffectsComponent>(target))
            {
                var markComp = EnsureComp<HereticCombatMarkComponent>(target);
                markComp.Path = hereticComp.MainPath;
            }
        }

        QueueDel(ent);
    }

    private void OnUseInHand(EntityUid uid, MansusGraspComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<HereticComponent>(args.User, out var hereticComp))
        {
            QueueDel(uid);
            return;
        }

        args.Handled = true;
        QueueDel(uid);
    }

    private void OnAfterInteract(Entity<TagComponent> ent, ref AfterInteractEvent args)
    {
        var tags = ent.Comp.Tags;

        if(!TryComp<HandsComponent>(args.User, out var userHands))
        {
            return;
        }

        if (!args.CanReach
        || !args.ClickLocation.IsValid(EntityManager)
        || !TryComp<HereticComponent>(args.User, out var heretic) // not a heretic - how???
        || !MansusGraspActive(args.User) && !(userHands.Count <= 1) // no grasp or no extra hand to make a grasp
        || HasComp<ActiveDoAfterComponent>(args.User) // prevent rune shittery
        || (!tags.Contains("Write") && !tags.Contains("DecapoidClaw")) // not a writing implement or decapoid claw
        || args.Target != null && HasComp<ItemComponent>(args.Target)) //don't allow clicking items (otherwise the circle gets stuck to them)
            return;

        // remove our rune if clicked
        if (args.Target != null && HasComp<HereticRitualRuneComponent>(args.Target))
        {
            // todo: add more fluff
            QueueDel(args.Target);
            args.Handled = true;
            return;
        }

        // spawn our rune
        var rune = Spawn("HereticRuneRitualDrawAnimation", args.ClickLocation);
        var dargs = new DoAfterArgs(EntityManager, args.User, 14f, new DrawRitualRuneDoAfterEvent(rune, args.ClickLocation), args.User)
        {
            BreakOnDamage = true,
            BreakOnHandChange = true,
            BreakOnMove = true,
            CancelDuplicate = false,
        };
        _doAfter.TryStartDoAfter(dargs);
    }
    private void OnRitualRuneDoAfter(Entity<HereticComponent> ent, ref DrawRitualRuneDoAfterEvent ev)
    {
        // delete the animation rune regardless
        QueueDel(ev.RitualRune);

        if (!ev.Cancelled)
            Spawn("HereticRuneRitual", ev.Coords);
    }
}
