using Content.Server._Impstation.CosmicCult.Components;
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
using Content.Shared.Popups;
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
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

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
        if (args.Target == null || !args.CanReach || !HasComp<MobStateComponent>(args.Target))
            return;

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
        if (args.Cancelled || args.Handled)
            return;
        if (args.Args.Target == null)
            return;
        var target = args.Args.Target;

        _popup.PopupEntity(Loc.GetString("cleanse-deconvert-attempt-success", ("target", Identity.Entity(target.Value, EntityManager))), uid, uid);
        if (_entMan.HasComponent<CosmicCultComponent>(args.Target))
        {
            var tgtpos = Transform(target.Value).Coordinates;
            // Spawn(uid.Comp.CleanseVFX, tgtpos);
            DeconvertCultist(target.Value);
            _audio.PlayPvs(uid.Comp.CleanseSound, tgtpos, AudioParams.Default.WithVolume(+6f));
        }
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
