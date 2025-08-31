using Content.Server.Atmos.Rotting;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.DoAfter;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Forensics;
using Content.Server.Popups;
using Content.Shared._Impstation.Kodepiia;
using Content.Shared._Impstation.Kodepiia.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Silicons.Laws.Components;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;

namespace Content.Server._Impstation.Kodepiia;

public sealed class KodepiiaConsumeSystem : SharedKodepiiaConsumeSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly RottingSystem _rotting = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly ForensicsSystem _forensics = default!;
    [Dependency] private readonly IngestionSystem _ingestion = default!;
    [Dependency] private readonly StomachSystem _stomach = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KodepiiaConsumeActionComponent, KodepiiaConsumeEvent>(Consume);
        SubscribeLocalEvent<KodepiiaConsumeActionComponent, KodepiiaConsumeDoAfterEvent>(ConsumeDoafter);
    }

    private bool KodepiiaTarget(EntityUid target)
    {
        if (!TryComp<RespiratorComponent>(target, out _))
            return false;
        if (!TryComp<BloodstreamComponent>(target, out _))
            return false;

        return !TryComp<SiliconLawBoundComponent>(target, out _);
    }

    public void Consume(Entity<KodepiiaConsumeActionComponent> ent, ref KodepiiaConsumeEvent args)
    {
        if (!_ingestion.HasMouthAvailable(args.Performer, args.Target))
            return;

        if (!KodepiiaTarget(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("kodepiia-consume-fail-inedible", ("target", Identity.Entity(args.Target, EntityManager))), ent, ent);
            return;
        }

        if (!_mobState.IsIncapacitated(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("kodepiia-consume-fail-incapacitated", ("target", Identity.Entity(args.Target, EntityManager))), ent, ent);
            return;
        }

        PlayMeatySound(ent);

        if (!TryComp<PhysicsComponent>(args.Target, out var targetPhysics))
            return;

        var doargs = new DoAfterArgs(EntityManager, ent, targetPhysics.Mass / 8, new KodepiiaConsumeDoAfterEvent(), ent, args.Target)
        {
            DistanceThreshold = 1.5f,
            BreakOnDamage = true,
            BreakOnHandChange = false,
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            AttemptFrequency = AttemptFrequency.StartAndEnd
        };

        var popupSelf = Loc.GetString("kodepiia-consume-start-self", ("user", Identity.Entity(ent, EntityManager)), ("target", Identity.Entity(args.Target, EntityManager)));
        var popupOthers = Loc.GetString("kodepiia-consume-start-others", ("user", Identity.Entity(ent, EntityManager)), ("target", Identity.Entity(args.Target, EntityManager)));

        _popup.PopupEntity(popupSelf, ent, ent);
        _popup.PopupEntity(popupOthers, ent, Filter.Pvs(ent).RemovePlayersByAttachedEntity(ent), true, PopupType.MediumCaution);

        _doAfter.TryStartDoAfter(doargs);
        args.Handled = true;
    }

    public void ConsumeDoafter(Entity<KodepiiaConsumeActionComponent> ent, ref KodepiiaConsumeDoAfterEvent args)
    {
        if (args.Target == null || args.Cancelled || !TryComp<PhysicsComponent>(args.Target, out var targetPhysics))
            return;

        if (!_body.TryGetBodyOrganEntityComps<StomachComponent>(ent.Owner, out var stomachs))
            return;

        var highestAvailable = FixedPoint2.Zero;
        Entity<StomachComponent>? stomachToUse = null;
        foreach (var stomach in stomachs)
        {
            var owner = stomach.Owner;
            if (!_solutionContainer.ResolveSolution(owner, "stomach", ref stomach.Comp1.Solution, out var stomachSol))
                continue;

            if (stomachSol.AvailableVolume <= highestAvailable)
                continue;

            stomachToUse = stomach;
            highestAvailable = stomachSol.AvailableVolume;
        }

        // All stomachs are full or we have no stomachs
        if (stomachToUse == null)
        {
            _popup.PopupClient(Loc.GetString("ingestion-you-cannot-ingest-any-more", ("verb", "eat")), ent, ent);
            return;
        }

        // Drink Bloodstream
        _solutionContainer.TryGetSolution(args.Target.Value, "bloodstream", out var targetSolutionComp, out var targetBloodstream);
        if (targetBloodstream != null && targetSolutionComp != null)
        {
            const float portionDrunk = 0.1f;
            var amountOfUncookedProtein = targetPhysics.Mass * 0.3f;

            var consumedSolution = _solutionContainer.SplitSolution(targetSolutionComp.Value, targetBloodstream.Volume * portionDrunk);

            if (_rotting.IsRotten(args.Target.Value))
            {
                consumedSolution.AddReagent("GastroToxin", amountOfUncookedProtein * 0.5f);
                amountOfUncookedProtein *= 0.5f;
            }

            consumedSolution.AddReagent("UncookedAnimalProteins", amountOfUncookedProtein);

            if (consumedSolution.Volume > highestAvailable)
            {
                var split = consumedSolution.SplitSolution(consumedSolution.Volume - highestAvailable);
                _puddle.TrySpillAt(ent.Owner, split, out var puddle);
            }
            _stomach.TryTransferSolution(stomachToUse.Value.Owner, consumedSolution, stomachToUse);
        }

        // Transfer DNA
        _forensics.TransferDna(args.Target.Value, ent, false);

        // Deal Damage
        _damage.TryChangeDamage(args.Target, ent.Comp.Damage, true, false);

        // Play Sound
        PlayMeatySound(ent);

        var popupSelf = Loc.GetString("kodepiia-consume-end-self", ("user", Identity.Entity(ent, EntityManager)), ("target", Identity.Entity(args.Target.Value, EntityManager)));
        var popupOthers = Loc.GetString("kodepiia-consume-end-others", ("user", Identity.Entity(ent, EntityManager)), ("target", Identity.Entity(args.Target.Value, EntityManager)));
        _popup.PopupEntity(popupSelf, ent, ent);
        _popup.PopupEntity(popupOthers, ent, Filter.Pvs(ent).RemovePlayersByAttachedEntity(ent), true, PopupType.LargeCaution);

        //Consumed Componentry Stuff lol
        EnsureComp<KodepiiaConsumedComponent>(args.Target.Value, out var consumed);
        consumed.TimesConsumed += 1;
        if (consumed.TimesConsumed >= 12 && TryComp<BodyComponent>(args.Target.Value, out var targetBody) && ent.Comp.CanGib)
            _body.GibBody(args.Target.Value,true,targetBody);
    }

    public void PlayMeatySound(Entity<KodepiiaConsumeActionComponent> ent)
    {
        var soundPool = new SoundCollectionSpecifier("gib");
        _audio.PlayPvs(soundPool, ent, AudioParams.Default.WithVolume(-3f));
    }
}
