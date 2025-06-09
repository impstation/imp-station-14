using Content.Server._Impstation.WashingMachine.Components;
using Content.Server.Administration.Logs;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Server.DeviceLinking.Systems;
using Content.Server.DoAfter;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Forensics;
using Content.Server.Jittering;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Storage.Components;
using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Shared._Impstation.EntityEffects.Effects;
using Content.Shared._Impstation.WashingMachine;
using Content.Shared.Atmos;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DoAfter;
using Content.Shared.Emag.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Forensics;
using Content.Shared.Jittering;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Server._Impstation.WashingMachine.EntitySystems;

/// <summary>
/// System used by washing machines to handle the washing of entities.
/// </summary>
/// <seealso cref="ChemicalWashingMachineAdapterComponent"/>
public sealed class WashingMachineSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly DizzySystem _dizzy = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly ForensicsSystem _forensics = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly TemperatureSystem _temperature = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WashingMachineComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<WashingMachineComponent, GetVerbsEvent<ActivationVerb>>(AddWashVerb);

        SubscribeLocalEvent<WashingMachineComponent, BreakageEventArgs>(OnBreak);
        SubscribeLocalEvent<WashingMachineComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<WashingMachineComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<WashingMachineComponent, GotEmaggedEvent>(OnEmagged);

        SubscribeLocalEvent<ActiveWashingMachineComponent, ComponentStartup>(OnWashStart);
        SubscribeLocalEvent<ActiveWashingMachineComponent, WashEndEvent>(OnWashEnd);
        SubscribeLocalEvent<ActiveWashingMachineComponent, ComponentShutdown>(OnWashStop);
        SubscribeLocalEvent<ActivelyWashedComponent, OnConstructionTemperatureEvent>(OnConstructionTemp);

        SubscribeLocalEvent<ChemicalWashingMachineAdapterComponent, WashingMachineGetReagentsEvent>(ChemicalGetReagents);
    }

    #region Startup

    private void OnMapInit(Entity<WashingMachineComponent> ent, ref MapInitEvent args)
    {
        _deviceLink.EnsureSinkPorts(ent, ent.Comp.OnPort);
    }
    private void AddWashVerb(Entity<WashingMachineComponent> ent, ref GetVerbsEvent<ActivationVerb> args) // if interacting gets annoying, change to alternativeverb
    {
        // We need an actor to give the verb.
        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        // Make sure ghosts can't use verb.
        if (!args.CanInteract)
            return;

        var washVerb = new ActivationVerb
        {
            Text = Loc.GetString("washing-verb-wash"),
            Act = () =>
            {
                // If either the actor or comp have magically vanished
                if (actor.PlayerSession.AttachedEntity == null || !HasComp<WashingMachineComponent>(ent))
                    return;

                TryStartWash(ent);
            },
            Impact = LogImpact.Low,
        };
        washVerb.Impact = LogImpact.Low;
        args.Verbs.Add(washVerb);
    }

    private void OnPowerChanged(Entity<WashingMachineComponent> ent, ref PowerChangedEvent args)
    {
        if (!args.Powered)
        {
            SetAppearance(ent.Owner, WashingMachineVisualState.Idle);
            StopWashing(ent);
        }
    }

    private void OnSignalReceived(Entity<WashingMachineComponent> ent, ref SignalReceivedEvent args)
    {
        if (args.Port != ent.Comp.OnPort)
            return;

        TryStartWash(ent);
    }

    private void OnBreak(Entity<WashingMachineComponent> ent, ref BreakageEventArgs args)
    {
        ent.Comp.Broken = true;
        SetAppearance(ent.Owner, WashingMachineVisualState.Broken);
        StopWashing(ent);

        if (TryComp<SolutionContainerManagerComponent>(ent, out var solComp))
        {
            _solutionContainer.TryGetSolution(solComp, "tank", out var solution);
            if (solution != null)
            {
                _puddle.TrySpillAt(Transform(ent).Coordinates, solution, out _);
            }
        }
    }

    #endregion
    #region Washing

    public void TryStartWash(Entity<WashingMachineComponent> ent)
    {
        if (ent.Comp.Broken)
        {
            _popupSystem.PopupEntity(Loc.GetString("washingmachine-component-interact-using-broken"), ent);
            return;
        }

        if (HasComp<ActiveWashingMachineComponent>(ent))
        {
            _popupSystem.PopupEntity(Loc.GetString("washing-popup-notify-wash-locked"), ent, PopupType.Large);
            return;
        }

        if (!(TryComp<ApcPowerReceiverComponent>(ent, out var apc) && apc.Powered))
        {
            _popupSystem.PopupEntity(Loc.GetString("washingmachine-component-interact-using-no-power"), ent);
            return;
        }

        if (TryComp<EntityStorageComponent>(ent, out var storage) && storage.Open)
        {
            _popupSystem.PopupEntity(Loc.GetString("washingmachine-component-interact-using-open"), ent);
            return;
        }

        if (GetReagents(ent).Item1 < ent.Comp.WaterRequired)
        {
            _popupSystem.PopupEntity(Loc.GetString("washingmachine-component-interact-using-no-water"), ent);
            return;
        }

        StartWash(ent);
    }

    /// <summary>
    /// Starts Washing. Use TryStartWash first!
    /// </summary>
    /// <remarks>
    /// im NOT fucking naming this wrhzhzh -mq
    /// </remarks>
    public void StartWash(Entity<WashingMachineComponent> ent)
    {
        if (HasComp<ActiveWashingMachineComponent>(ent) || !(TryComp<ApcPowerReceiverComponent>(ent, out var apc) && apc.Powered))
            return;

        var malfunctioning = false;

        EnsureComp<EntityStorageComponent>(ent, out var storeComp); // if we got this far we should already have the thing
        storeComp.Openable = false;
        foreach (var item in storeComp.Contents.ContainedEntities.ToArray())
        {
            var ev = new ActivelyBeingWashedEvent(ent);
            RaiseLocalEvent(item, ev);

            if (_tag.HasTag(item, "Lint") || _tag.HasTag(item, "Metal"))
                malfunctioning = true;

            if (TryComp<BodyComponent>(item, out _))
                _dizzy.MakeDizzy(item, ent.Comp.WashTimerTime * ent.Comp.DizzyMultiplier);

            var activeWashedComp = AddComp<ActivelyWashedComponent>(item);
            activeWashedComp.WashingMachine = ent.Owner;
        }

        _audio.PlayPvs(ent.Comp.StartWashingSound, ent);
        var activeComp = AddComp<ActiveWashingMachineComponent>(ent); // washing machine is go!
        activeComp.WashTimeRemaining = ent.Comp.WashTimerTime;
        activeComp.TotalTime = ent.Comp.WashTimerTime; //this doesn't scale so that we can have the "actual" time
        ent.Comp.CurrentWashTimeEnd = _gameTiming.CurTime + TimeSpan.FromSeconds(ent.Comp.WashTimerTime);
        if (malfunctioning)
            activeComp.MalfunctionTime = _gameTiming.CurTime + TimeSpan.FromSeconds(ent.Comp.MalfunctionInterval);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveWashingMachineComponent, WashingMachineComponent>();

        while (query.MoveNext(out var uid, out var active, out var wash))
        {
            active.WashTimeRemaining -= frameTime;
            var tileMix = _atmos.GetTileMixture(uid, excite: true);

            RollMalfunction((uid, active, wash), tileMix);

            //check if there's still wash time left
            if (active.WashTimeRemaining > 0)
            {
                AddWashDamage(uid, frameTime);
                tileMix?.AdjustMoles(Gas.WaterVapor, wash.BaseSteamOutput);
                continue;
            }

            //this means the cycle has finished.
            AddWashDamage(uid, Math.Max(frameTime + active.WashTimeRemaining, 0)); //Though there's still a little bit more heat to pump out
            RaiseLocalEvent(uid, new WashEndEvent(uid));
        }
    }

    /// <summary>
    ///     Adds temperature &/or damage to every item in the washing machine,
    ///     based on the time it took to wash.
    /// </summary>
    /// <param name="washComp">The washing machine that is heating up.</param>
    /// <param name="storeComp">The entity storage attached to the washing machine.</param>
    /// <param name="time">The time on the washing machine, in seconds.</param>
    private void AddWashDamage(Entity<WashingMachineComponent?, EntityStorageComponent?> ent, float time)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2))
            return;

        var heatToAdd = time * ent.Comp1.BaseHeatMultiplier;
        var damageToDo = time * ent.Comp1.Damage;
        var emaggedDamage = damageToDo;
        DamageSpecifier meleeDamage = new();
        foreach (var entity in ent.Comp2.Contents.ContainedEntities)
        {
            if (TryComp<TemperatureComponent>(entity, out var tempComp))
                _temperature.ChangeHeat(entity, heatToAdd * ent.Comp1.ObjectHeatMultiplier, false, tempComp);

            if (TryComp<SolutionContainerManagerComponent>(entity, out var solutions))
                foreach (var (_, soln) in _solutionContainer.EnumerateSolutions((entity, solutions)))
                {
                    var solution = soln.Comp.Solution;
                    if (solution.Temperature > ent.Comp1.TemperatureUpperThreshold)
                        break;

                    _solutionContainer.AddThermalEnergy(soln, heatToAdd);
                }
            // ideally if emagged, the entity should be crit (half dead) JUST as the cycle ends. theyre getting gibbed anyway so the specifics dont matter.
            if (_emag.CheckFlag(ent, EmagType.Interaction)
            && TryComp<MobThresholdsComponent>(entity, out var threshold)
            && TryComp<DamageableComponent>(entity, out var damageable))
            {
                foreach (var damage in damageToDo.DamageDict)
                    emaggedDamage.DamageDict[damage.Key] = (threshold.Thresholds.Keys.Last() - damageable.TotalDamage) / 2 * time;
                damageToDo = emaggedDamage;
            }
            _damageable.TryChangeDamage(entity, damageToDo);

            if (TryComp<MeleeWeaponComponent>(entity, out var melee) && !melee.MustBeEquippedToUse) // dont wash knives!!!
                if (_random.Prob(ent.Comp1.WeaponHitChance))
                {
                    meleeDamage += melee.Damage;
                    _audio.PlayPvs(melee.HitSound, ent);
                }
        }
        _damageable.TryChangeDamage(ent, meleeDamage);
    }

    #endregion
    #region Malfunction

    /// <summary>
    /// Handles the attempted washing of dryer lint
    /// </summary>
    /// <remarks>
    /// Returns false if the washing machine didn't explode, true if it exploded.
    /// </remarks>
    private void RollMalfunction(Entity<ActiveWashingMachineComponent, WashingMachineComponent> ent, GasMixture? tileMix)
    {
        if (ent.Comp1.MalfunctionTime == TimeSpan.Zero)
            return;

        if (ent.Comp1.MalfunctionTime > _gameTiming.CurTime)
            return;

        ent.Comp1.MalfunctionTime = _gameTiming.CurTime + TimeSpan.FromSeconds(ent.Comp2.MalfunctionInterval);
        if (_random.Prob(ent.Comp2.ExplosionChance))
        {
            Explode((ent, ent.Comp2));
            return;  // washing machine broke, stop cycle
        }

        if (_random.Prob(ent.Comp2.SteamChance))
        {
            tileMix?.AdjustMoles(Gas.WaterVapor, ent.Comp2.BaseSteamOutput * ent.Comp2.MalfunctionSteamMultiplier);
        }
    }

    /// <summary>
    /// Explodes the washing machine internally, turning it into a broken state, destroying its board, and spitting out its machine parts
    /// </summary>
    /// <param name="ent"></param>
    public void Explode(Entity<WashingMachineComponent> ent)
    {
        ent.Comp.Broken = true; // Make broken so we stop processing stuff
        _explosion.TriggerExplosive(ent); // are microwaves explosive???
        if (TryComp<MachineComponent>(ent, out var machine))
        {
            _container.CleanContainer(machine.BoardContainer);
            _container.EmptyContainer(machine.PartContainer);
        }

        _adminLogger.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(ent)} exploded from unsafe washing!");
    }

    public void DoGibBehaviour(Entity<WashingMachineComponent?> ent, List<EntityUid> storage)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;
        foreach (var user in storage)
            if (TryComp<BodyComponent>(user, out _))
                _body.GibBody(user, true);
        ent.Comp.Bloody = true;
        SetAppearance(ent, WashingMachineVisualState.Bloody);
    }

    #endregion
    #region Helpers

    public void SetAppearance(Entity<WashingMachineComponent?, AppearanceComponent?> ent, WashingMachineVisualState state)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2, false))
            return;

        // we want to prioritize setting layers to broken first.
        // door MIGHT be bloodied, if it is we should keep it bloodied. broken should be prioritized over bloodied.
        var breakOrState = ent.Comp1.Broken ? WashingMachineVisualState.Broken : state;
        foreach (var layer in Enum.GetValues<WashingMachineVisualLayers>())
            _appearance.SetData(ent, layer, breakOrState);
        if (ent.Comp1.Bloody && !ent.Comp1.Broken)
            _appearance.SetData(ent, WashingMachineVisualLayers.Door, WashingMachineVisualState.Bloody);

        // handling lights when unpowered
        if (TryComp<ApcPowerReceiverComponent>(ent, out var power))
            _appearance.SetData(ent, PowerDeviceVisuals.VisualState, power.Powered);
    }

    private void OnWashStart(Entity<ActiveWashingMachineComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<WashingMachineComponent>(ent, out var comp))
            return;
        SetAppearance(ent.Owner, WashingMachineVisualState.Active);

        _jitter.AddJitter(ent, -2, 80);
        comp.PlayingStream =
            _audio.PlayPvs(comp.LoopingSound, ent, AudioParams.Default.WithLoop(true).WithMaxDistance(5))?.Entity;
    }

    private void OnWashEnd(Entity<ActiveWashingMachineComponent> ent, ref WashEndEvent args)
    {
        if (!TryComp<WashingMachineComponent>(ent.Owner, out var wash))
            return;

        if (!TryComp<EntityStorageComponent>(ent.Owner, out var storage))
            return;
        var ents = storage.Contents.ContainedEntities.ToList();
        if (_emag.CheckFlag(ent.Owner, EmagType.Interaction))
            DoGibBehaviour(ent.Owner, ents);

        if (TryComp<CleansForensicsComponent>(ent.Owner, out _))
            foreach (var item in ents)
            {
                var doAfterArgs = new DoAfterArgs(EntityManager, ent.Owner, TimeSpan.Zero, new CleanForensicsDoAfterEvent(), ent, target: item, used: ent);
                _doAfter.TryStartDoAfter(doAfterArgs); // this just begs for a forensics refactor
            }

        if (_random.Prob(wash.LintChance))
            Spawn(wash.LintPrototype, Transform(ent).Coordinates);
        StopWashing((ent.Owner, wash));
        ChemicalUseReagent(ent.Owner, false);
        if (!wash.Broken)
            _audio.PlayPvs(wash.CycleDoneSound, ent.Owner);
    }

    // StopWashing is called whenever washing stops abruptly, in addition to when it naturally ends
    private void StopWashing(Entity<WashingMachineComponent> ent)
    {
        RemCompDeferred<ActiveWashingMachineComponent>(ent);
        if (TryComp<EntityStorageComponent>(ent, out var comp))
        {
            foreach (var item in comp.Contents.ContainedEntities)
                RemCompDeferred<ActivelyWashedComponent>(item);
            comp.Openable = true;
            _container.EmptyContainer(comp.Contents);
        }
        ent.Comp.CurrentWashTimeEnd = TimeSpan.Zero;
    }
    private void OnWashStop(Entity<ActiveWashingMachineComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<WashingMachineComponent>(ent, out var comp))
            return;

        SetAppearance(ent.Owner, WashingMachineVisualState.Idle);
        RemComp<JitteringComponent>(ent);
        comp.PlayingStream = _audio.Stop(comp.PlayingStream);
    }


    // Stop items from transforming through constructiongraphs while being washed.
    // They might be reserved for dyes.
    private void OnConstructionTemp(Entity<ActivelyWashedComponent> ent, ref OnConstructionTemperatureEvent args)
    {
        args.Result = HandleResult.False;
    }

    private void OnEmagged(Entity<WashingMachineComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(ent, EmagType.Interaction))
            return;

        args.Handled = true;
    }

    #endregion
    #region Reagent Helpers
    public (FixedPoint2, FixedPoint2) GetReagents(EntityUid washing)
    {
        WashingMachineGetReagentsEvent getReagentsEvent = default;
        RaiseLocalEvent(washing, ref getReagentsEvent);
        return (getReagentsEvent.Water, getReagentsEvent.Cleaner);
    }

    private void ChemicalGetReagents(Entity<ChemicalWashingMachineAdapterComponent> ent, ref WashingMachineGetReagentsEvent args)
    {
        if (!_solutionContainer.ResolveSolution(ent.Owner, ent.Comp.SolutionName, ref ent.Comp.Solution, out var solution))
            return;

        FixedPoint2 water = 0;
        FixedPoint2 clean = 0;
        foreach (var (reagentId, multiplier) in ent.Comp.ReagentWater)
        {
            var reagent = solution.GetTotalPrototypeQuantity(reagentId);
            reagent += ent.Comp.FractionalReagents.GetValueOrDefault(reagentId) * FixedPoint2.Epsilon;

            water += reagent * multiplier;
        }
        foreach (var (reagentId, multiplier) in ent.Comp.ReagentCleaner)
        {
            var reagent = solution.GetTotalPrototypeQuantity(reagentId);
            reagent += ent.Comp.FractionalReagents.GetValueOrDefault(reagentId) * FixedPoint2.Epsilon;

            clean += reagent * multiplier;
        }

        args.Water = water;
        args.Cleaner = clean;
    }

    /// <summary>
    /// Draw water/cleaning agent from its adapters.
    /// </summary>
    /// <remarks>
    /// If Cleaning == false, water will be used.
    /// If Cleaning == true, cleaning agent will be used.
    /// </remarks>
    public void ChemicalUseReagent(Entity<ChemicalWashingMachineAdapterComponent?> ent, bool cleaning)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;
        if (!_solutionContainer.ResolveSolution(ent.Owner, ent.Comp.SolutionName, ref ent.Comp.Solution, out var solution))
            return;

        if (!TryComp<WashingMachineComponent>(ent, out var wash))
            return;

        var reagentUsed = ent.Comp.ReagentWater;
        var reagentRequired = wash.WaterRequired;
        if (cleaning)
        {
            reagentUsed = ent.Comp.ReagentCleaner;
            reagentRequired = wash.CleanerRequired;
        }

        var totalReagent = 0f;
        foreach (var (reagentId, _) in reagentUsed)
        {
            totalReagent += solution.GetTotalPrototypeQuantity(reagentId).Float();
            totalReagent += ent.Comp.FractionalReagents.GetValueOrDefault(reagentId);
        }

        if (totalReagent == 0)
            return;

        foreach (var (reagentId, _) in reagentUsed)
        {
            _solutionContainer.RemoveReagent(ent.Comp.Solution.Value, reagentId, reagentRequired);
        }
    }

    /// <summary>
    /// Raised by <see cref="WashingMachineSystem"/> to calculate the amount of water &/or cleaning agent in the tank.
    /// </summary>
    [ByRefEvent]
    public record struct WashingMachineGetReagentsEvent(FixedPoint2 Water, FixedPoint2 Cleaner);

    #endregion
}
