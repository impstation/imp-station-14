using Content.Server._Impstation.CosmicCult.Components;
using Content.Server.Bible.Components;
using Content.Server.Body.Components;
using Content.Server.Medical.Components;
using Content.Server.PowerCell;
using Content.Server.Radio.Components;
using Content.Server.Temperature.Components;
using Content.Server.Traits.Assorted;
using Content.Shared._Impstation.CosmicCult;
using Content.Shared._Impstation.CosmicCult.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Jittering;
using Content.Shared.MedicalScanner;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server._Impstation.CosmicCult;

public sealed class CleanseDeconversionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly UseDelaySystem _delay = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CleanseOnUseComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<CleanseOnUseComponent, CleanseOnDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<CleanseCultComponent, ComponentInit>(OnCompInit);
    }
    private void OnCompInit(Entity<CleanseCultComponent> ent, ref ComponentInit args)
    {
        var duration = TimeSpan.FromSeconds(60.0f);
        _entMan.System<SharedJitteringSystem>().DoJitter(ent.Owner, duration, true, 5, 20);
        ent.Comp.CleanseTime = _timing.CurTime + duration;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var deconCultTimer = EntityQueryEnumerator<CleanseCultComponent>();
        while (deconCultTimer.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime >= comp.CleanseTime)
            {
                _popup.PopupEntity(Loc.GetString("cosmicability-blank-return"), uid, uid);
                RemComp<CleanseCultComponent>(uid);
                DeconvertCultist(uid);
            }
        }
    }

    private void OnAfterInteract(Entity<CleanseOnUseComponent> uid, ref AfterInteractEvent args)
    {
        if (!TryComp(uid, out UseDelayComponent? useDelay) || _delay.IsDelayed((uid, useDelay)))
            return;
        if (!args.CanReach || args.Target == null || !_mobState.IsAlive(args.Target.Value))
            return;

        if (!HasComp<BibleUserComponent>(args.User))
        {
            _popup.PopupEntity(Loc.GetString("cleanse-item-sizzle", ("target", Identity.Entity(args.Used, EntityManager))), args.User, args.User);

            _audio.PlayPvs(uid.Comp.SizzleSound, args.User);
            _damageable.TryChangeDamage(args.User, uid.Comp.SelfDamage, origin: uid);
            _delay.TryResetDelay((uid, useDelay));
            return;
        }

        _popup.PopupEntity(Loc.GetString("cleanse-deconvert-attempt-begin", ("target", Identity.Entity(args.User, EntityManager))), args.User, args.Target.Value);
        _popup.PopupEntity(Loc.GetString("cleanse-deconvert-attempt-begin-user", ("target", Identity.Entity(args.Target.Value, EntityManager))), args.User, args.User);

        var doAfterCancelled = !_doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, uid.Comp.UseTime, new CleanseOnDoAfterEvent(), uid, target: args.Target, used: uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            DistanceThreshold = 1.5f,
            RequireCanInteract = true,
            BlockDuplicate = true,
            CancelDuplicate = true,
            NeedHand = true,
        });

        if (args.Target == args.User || doAfterCancelled)
            return;
    }

    private void OnDoAfter(Entity<CleanseOnUseComponent> uid, ref CleanseOnDoAfterEvent args)
    {
        var target = args.Args.Target;
        if (!TryComp(uid, out UseDelayComponent? useDelay) || args.Cancelled || args.Handled || target == null || !_mobState.IsAlive(target.Value))
            return;
        //TODO: This could be made more agnostic, but there's only one cult for now, and frankly, i'm fucking tired. This is easy to read and easy to modify code. Expand it at thine leisure.

        if (_entMan.HasComponent<CosmicCultComponent>(args.Target))
        {
            var tgtpos = Transform(target.Value).Coordinates;
            Spawn(uid.Comp.CleanseVFX, tgtpos);
            DeconvertCultist(target.Value);
            _audio.PlayPvs(uid.Comp.CleanseSound, tgtpos, AudioParams.Default.WithVolume(+6f));
            _popup.PopupEntity(Loc.GetString("cleanse-deconvert-attempt-success", ("target", Identity.Entity(target.Value, EntityManager))), args.User, args.User);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("cleanse-deconvert-attempt-notcult", ("target", Identity.Entity(target.Value, EntityManager))), args.User, args.User);
        }
        _delay.TryResetDelay((uid, useDelay));
        args.Handled = true;
    }

    public void DeconvertCultist(EntityUid uid)
    {
        if (HasComp<CosmicCultComponent>(uid))
        {
            RemComp<CosmicCultComponent>(uid);
            RemComp<ActiveRadioComponent>(uid);
            RemComp<IntrinsicRadioReceiverComponent>(uid);
            RemComp<IntrinsicRadioTransmitterComponent>(uid);

            if (HasComp<CosmicCultLeadComponent>(uid))
                RemComp<CosmicCultLeadComponent>(uid);
        }
    }
}
