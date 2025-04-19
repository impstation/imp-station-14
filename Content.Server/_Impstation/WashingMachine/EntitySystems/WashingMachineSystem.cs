using Content.Server.Administration.Logs;
using Content.Server.Body.Systems;
using Content.Server.Construction;
using Content.Server.Explosion.EntitySystems;
using Content.Server.DeviceLinking.Events;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Hands.Systems;
using Content.Server.Kitchen.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Robust.Shared.Random;
using Robust.Shared.Audio;
using Content.Server.Lightning;
using Content.Shared.Item;
using Content.Shared.Kitchen;
using Content.Shared.Kitchen.Components;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using System.Linq;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.Stacks;
using Content.Server.Construction.Components;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Robust.Shared.Utility;

using Content.Server._Impstation.WashingMachine.Components;
using Content.Shared._Impstation.WashingMachine.Components;
using Content.Shared.Storage.Components;
using Content.Shared.Chemistry.Components;
using Content.Server.Fluids.EntitySystems;


// The goal here, for mq:
//
// WASHING MACHINE
// - is a crate you can put literally anything in
// - on activate, locks the open function and washes things in it
//      crayons dye 'dyeable' entities
//      maybe it also converts some entities into other entities
// what happens to a player in the washing machine? well they get fucking washed.
//      heat damage? blunt damage? not enough to ash paper. but a BIT
//      emag: it does More Damage Lots
// probably spawns a little vapor at the end (maybe adds heat? idk man)
//
// microwaves need water & power to function.
//      a certain amount of water will be used up by the microwave.
// soap, bleach, space cleaner... anything with a dyeremover component, maybe? can remove colours from entities.
// washing something will remove dna and fingerprints, and add 'lint' fibers
//
// no ui. if i end up stabbing myself in the foot with this one, so be it.
//
// GAMEPLAY GOALS
// - dyeing shit, obviously
// - putting nukies in the tumble dryer
// - hiding place for sneakthieves
// - "why is this air alarm going off" "someone left the dryer on"
// - washing machine microwavable egg
//
// CHECKLIST
// [] WashingMachineComponent
// [] ActiveWashingMachineComponent
// [X] ActivelyWashedComponent
// [] WashingMachineSystem
// [X] SharedWashingMachine
// [] DyeableComponent
// [] DyeRemover tag
// [] 'Dizzy' status effect
// [] YAML edits

namespace Content.Server._Impstation.WashingMachine.EntitySystems
{
    public sealed class WashingMachineSystem : EntitySystem
    {
        [Dependency] private readonly BodySystem _bodySystem = default!;
        [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly PowerReceiverSystem _power = default!;
        [Dependency] private readonly RecipeManager _recipeManager = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly LightningSystem _lightning = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly ExplosionSystem _explosion = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
        [Dependency] private readonly TagSystem _tag = default!;
        [Dependency] private readonly TemperatureSystem _temperature = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
        [Dependency] private readonly HandsSystem _handsSystem = default!;
        [Dependency] private readonly SharedItemSystem _item = default!;
        [Dependency] private readonly SharedStackSystem _stack = default!;
        [Dependency] private readonly IPrototypeManager _prototype = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly SharedSuicideSystem _suicide = default!;
        [Dependency] private readonly PuddleSystem _puddle = default!;

        [ValidatePrototypeId<EntityPrototype>]
        private const string MalfunctionSpark = "Spark";

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<WashingMachineComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<WashingMachineComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<WashingMachineComponent, StorageAfterOpenEvent>(OnWashDoorOpen);
            SubscribeLocalEvent<WashingMachineComponent, StorageAfterCloseEvent>(OnWashDoorClose);
            SubscribeLocalEvent<WashingMachineComponent, InteractUsingEvent>(OnInteractUsing, after: new[] { typeof(AnchorableSystem) });
            SubscribeLocalEvent<WashingMachineComponent, BreakageEventArgs>(OnBreak);
            SubscribeLocalEvent<WashingMachineComponent, PowerChangedEvent>(OnPowerChanged);

            SubscribeLocalEvent<WashingMachineComponent, SignalReceivedEvent>(OnSignalReceived);

            SubscribeLocalEvent<WashingMachineComponent, WashingMachineStartMessage>((u, c, m) => StartWash(u, c, m.Actor));
            SubscribeLocalEvent<ActiveWashingMachineComponent, ComponentStartup>(OnWashStart);
            SubscribeLocalEvent<ActiveWashingMachineComponent, ComponentShutdown>(OnWashStop);

            SubscribeLocalEvent<ActivelyWashedComponent, OnConstructionTemperatureEvent>(OnConstructionTemp);

            SubscribeLocalEvent<FoodRecipeProviderComponent, GetSecretRecipesEvent>(OnGetSecretRecipes);
        }

        private void OnWashStart(Entity<ActiveWashingMachineComponent> ent, ref ComponentStartup args)
        {
            if (!TryComp<WashingMachineComponent>(ent, out var comp))
                return;
            SetAppearance(ent.Owner, WashingMachineVisualState.Active, comp);

            comp.PlayingStream =
                _audio.PlayPvs(comp.LoopingSound, ent, AudioParams.Default.WithLoop(true).WithMaxDistance(5))?.Entity;
        }

        private void OnWashStop(Entity<ActiveWashingMachineComponent> ent, ref ComponentShutdown args)
        {
            if (!TryComp<WashingMachineComponent>(ent, out var comp))
                return;

            SetAppearance(ent.Owner, WashingMachineVisualState.Idle, comp);
            comp.PlayingStream = _audio.Stop(comp.PlayingStream);
        }

        // Stop items from transforming through constructiongraphs while being washed.
        // They might be reserved for dyes.
        private void OnConstructionTemp(Entity<ActivelyWashedComponent> ent, ref OnConstructionTemperatureEvent args)
        {
            args.Result = HandleResult.False;
        }

        /// <summary>
        ///     Adds temperature to every item in the washing machine,
        ///     based on the time it took to wash.
        /// </summary>
        /// <param name="component">The washing machine that is heating up.</param>
        /// <param name="time">The time on the washing machine, in seconds.</param>
        private void AddTemperature(WashingMachineComponent component, float time)
        {
            var heatToAdd = time * component.BaseHeatMultiplier;
            foreach (var entity in component.Contents.ContainedEntities) // unsure if this is ok with crate?
            {
                if (TryComp<TemperatureComponent>(entity, out var tempComp))
                    _temperature.ChangeHeat(entity, heatToAdd * component.ObjectHeatMultiplier, false, tempComp);

                if (!TryComp<SolutionContainerManagerComponent>(entity, out var solutions))
                    continue;
                foreach (var (_, soln) in _solutionContainer.EnumerateSolutions((entity, solutions)))
                {
                    var solution = soln.Comp.Solution;
                    if (solution.Temperature > component.TemperatureUpperThreshold)
                        continue;

                    _solutionContainer.AddThermalEnergy(soln, heatToAdd);
                }
            }
        }

        private void SubtractContents(MicrowaveComponent component, FoodRecipePrototype recipe) // LATER
        {
            // TODO Turn recipe.IngredientsReagents into a ReagentQuantity[]

            var totalReagentsToRemove = new Dictionary<string, FixedPoint2>(recipe.IngredientsReagents);

            // this is spaghetti ngl
            foreach (var item in component.Storage.ContainedEntities)
            {
                // use the same reagents as when we selected the recipe
                if (!_solutionContainer.TryGetDrainableSolution(item, out var solutionEntity, out var solution))
                    continue;

                foreach (var (reagent, _) in recipe.IngredientsReagents)
                {
                    // removed everything
                    if (!totalReagentsToRemove.ContainsKey(reagent))
                        continue;

                    var quant = solution.GetTotalPrototypeQuantity(reagent);

                    if (quant >= totalReagentsToRemove[reagent])
                    {
                        quant = totalReagentsToRemove[reagent];
                        totalReagentsToRemove.Remove(reagent);
                    }
                    else
                    {
                        totalReagentsToRemove[reagent] -= quant;
                    }

                    _solutionContainer.RemoveReagent(solutionEntity.Value, reagent, quant);
                }
            }

            foreach (var recipeSolid in recipe.IngredientsSolids)
            {
                for (var i = 0; i < recipeSolid.Value; i++)
                {
                    foreach (var item in component.Storage.ContainedEntities)
                    {
                        string? itemID = null;

                        // If an entity has a stack component, use the stacktype instead of prototype id
                        if (TryComp<StackComponent>(item, out var stackComp))
                        {
                            itemID = _prototype.Index<StackPrototype>(stackComp.StackTypeId).Spawn;
                        }
                        else
                        {
                            var metaData = MetaData(item);
                            if (metaData.EntityPrototype == null)
                            {
                                continue;
                            }
                            itemID = metaData.EntityPrototype.ID;
                        }

                        if (itemID != recipeSolid.Key)
                        {
                            continue;
                        }

                        if (stackComp is not null)
                        {
                            if (stackComp.Count == 1)
                            {
                                _container.Remove(item, component.Storage);
                            }
                            _stack.Use(item, 1, stackComp);
                            break;
                        }
                        else
                        {
                            _container.Remove(item, component.Storage);
                            Del(item);
                            break;
                        }
                    }
                }
            }
        }

        private void OnInit(Entity<WashingMachineComponent> ent, ref ComponentInit args)
        {
            // this really does have to be in ComponentInit
            ent.Comp.Contents = _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);
        }

        private void OnMapInit(Entity<WashingMachineComponent> ent, ref MapInitEvent args)
        {
            _deviceLink.EnsureSinkPorts(ent, ent.Comp.OnPort);
        }

        private void OnWashDoorClose(Entity<MicrowaveComponent> ent, ref StorageAfterCloseEvent args) // handle w crate
        {
            // add entitystorage contents to washingmachine contents (is this redundant?)
        }

        private void OnInteractUsing(Entity<WashingMachineComponent> ent, ref InteractUsingEvent args)
        {
            if (args.Handled)
                return;
            if (!(TryComp<ApcPowerReceiverComponent>(ent, out var apc) && apc.Powered)) // these 2 args should be moved to trywash or whatever
            {
                _popupSystem.PopupEntity(Loc.GetString("washingmachine-component-interact-using-no-power"), ent, args.User);
                return;
            }

            if (ent.Comp.Broken)
            {
                _popupSystem.PopupEntity(Loc.GetString("washingmachine-component-interact-using-broken"), ent, args.User);
                return;
            }

            if (ent.Comp.Storage.Count >= ent.Comp.Capacity)
            {
                _popupSystem.PopupEntity(Loc.GetString("microwave-component-interact-full"), ent, args.User);
                return;
            }

            args.Handled = true;
            _handsSystem.TryDropIntoContainer(args.User, args.Used, ent.Comp.Storage);
            UpdateUserInterfaceState(ent, ent.Comp); // no ui, handle w crate later
        }

        private void OnBreak(Entity<WashingMachineComponent> ent, ref BreakageEventArgs args)
        {
            ent.Comp.Broken = true;
            SetAppearance(ent, WashingMachineVisualState.Broken, ent.Comp);
            StopWashing(ent);
            _container.EmptyContainer(ent.Comp.Contents);

            _solutionContainer.TryGetSolution(ent, "tank", out var solution); //fuck!!!!!!!!
            if (solution != null)
            {
                Entity<SolutionComponent> solutions = (Entity<SolutionComponent>)solution;
                _puddle.TrySpillAt(Transform(ent).Coordinates, solutions.Comp.Solution, out _);
            }
        }

        private void OnPowerChanged(Entity<WashingMachineComponent> ent, ref PowerChangedEvent args)
        {
            if (!args.Powered)
            {
                SetAppearance(ent, WashingMachineVisualState.Idle, ent.Comp);
                StopWashing(ent);
            }
        }

        private void OnSignalReceived(Entity<MicrowaveComponent> ent, ref SignalReceivedEvent args)
        {
            if (args.Port != ent.Comp.OnPort)
                return;

            if (ent.Comp.Broken || !_power.IsPowered(ent))
                return;

            StartWash(ent.Owner, ent.Comp, null);
        }

        public void SetAppearance(EntityUid uid, WashingMachineVisualState state, WashingMachineComponent? component = null, AppearanceComponent? appearanceComponent = null)
        {
            if (!Resolve(uid, ref component, ref appearanceComponent, false))
                return;
            var display = component.Broken ? WashingMachineVisualState.Broken : state;
            _appearance.SetData(uid, PowerDeviceVisuals.VisualState, display, appearanceComponent);
        }

        public static bool HasContents(MicrowaveComponent component) // handle w crate
        {
            return component.Storage.ContainedEntities.Any();
        }

        // dunno if we wanna keep explosion shit as is or not, look at later

        /// <summary>
        /// Explodes the microwave internally, turning it into a broken state, destroying its board, and spitting out its machine parts
        /// </summary>
        /// <param name="ent"></param>
        public void Explode(Entity<MicrowaveComponent> ent)
        {
            ent.Comp.Broken = true; // Make broken so we stop processing stuff
            _explosion.TriggerExplosive(ent);
            if (TryComp<MachineComponent>(ent, out var machine))
            {
                _container.CleanContainer(machine.BoardContainer);
                _container.EmptyContainer(machine.PartContainer);
            }

            _adminLogger.Add(LogType.Action, LogImpact.Medium,
                $"{ToPrettyString(ent)} exploded from unsafe cooking!");
        }
        /// <summary>
        /// Handles the attempted cooking of unsafe objects
        /// </summary>
        /// <remarks>
        /// Returns false if the microwave didn't explode, true if it exploded.
        /// </remarks>
        private void RollMalfunction(Entity<ActiveMicrowaveComponent, MicrowaveComponent> ent)
        {
            if (ent.Comp1.MalfunctionTime == TimeSpan.Zero)
                return;

            if (ent.Comp1.MalfunctionTime > _gameTiming.CurTime)
                return;

            ent.Comp1.MalfunctionTime = _gameTiming.CurTime + TimeSpan.FromSeconds(ent.Comp2.MalfunctionInterval);
            if (_random.Prob(ent.Comp2.ExplosionChance))
            {
                Explode((ent, ent.Comp2));
                return;  // microwave is fucked, stop the cooking.
            }

            if (_random.Prob(ent.Comp2.LightningChance))
                _lightning.ShootRandomLightnings(ent, 1.0f, 2, MalfunctionSpark, triggerLightningEvents: false);
        }

        /// <summary>
        /// Starts Washing
        /// </summary>
        /// <remarks>
        /// the microwave version was named 'Wzhzhzh' im turning into joker -mq
        /// </remarks>
        public void StartWash(EntityUid uid, WashingMachineComponent component, EntityUid? user)
        {
            if (!HasContents(component) || HasComp<ActiveWashingMachineComponent>(uid) || !(TryComp<ApcPowerReceiverComponent>(uid, out var apc) && apc.Powered))
                return;

            var solidsDict = new Dictionary<string, int>(); // yeah later issue for me
            var reagentDict = new Dictionary<string, FixedPoint2>();
            var malfunctioning = false;
            // TODO use lists of Reagent quantities instead of reagent prototype ids.
            foreach (var item in component.Storage.ContainedEntities.ToArray())
            {
                var ev = new BeingWashedEvent(uid, user);
                RaiseLocalEvent(item, ev);

                if (ev.Handled)
                {
                    UpdateUserInterfaceState(uid, component);
                    return;
                }

                if (_tag.HasTag(item, "Metal")) // OH FUCK, TAGS
                {
                    malfunctioning = true;
                }

                if (_tag.HasTag(item, "Plastic"))
                {
                    var junk = Spawn(component.BadRecipeEntityId, Transform(uid).Coordinates);
                    _container.Insert(junk, component.Storage);
                    Del(item);
                    continue;
                }

                var activeWashedComp = AddComp<ActivelyWashedComponent>(item);
                activeWashedComp.WashingMachine = uid;

                string? solidID = null;
                int amountToAdd = 1;

                // If a microwave recipe uses a stacked item, use the default stack prototype id instead of prototype id
                if (TryComp<StackComponent>(item, out var stackComp))
                {
                    solidID = _prototype.Index<StackPrototype>(stackComp.StackTypeId).Spawn;
                    amountToAdd = stackComp.Count;
                }
                else
                {
                    var metaData = MetaData(item); //this simply begs for cooking refactor
                    if (metaData.EntityPrototype is not null)
                        solidID = metaData.EntityPrototype.ID;
                }

                if (solidID is null)
                    continue;

                if (!solidsDict.TryAdd(solidID, amountToAdd))
                    solidsDict[solidID] += amountToAdd;

                // only use reagents we have access to
                // you have to break the eggs before we can use them!
                if (!_solutionContainer.TryGetDrainableSolution(item, out var _, out var solution))
                    continue;

                foreach (var (reagent, quantity) in solution.Contents)
                {
                    if (!reagentDict.TryAdd(reagent.Prototype, quantity))
                        reagentDict[reagent.Prototype] += quantity;
                }
            }

            // Check recipes
            var getRecipesEv = new GetSecretRecipesEvent();
            RaiseLocalEvent(uid, ref getRecipesEv);

            List<FoodRecipePrototype> recipes = getRecipesEv.Recipes;
            recipes.AddRange(_recipeManager.Recipes);
            var portionedRecipe = recipes.Select(r =>
                CanSatisfyRecipe(component, r, solidsDict, reagentDict)).FirstOrDefault(r => r.Item2 > 0);

            _audio.PlayPvs(component.StartCookingSound, uid);
            var activeComp = AddComp<ActiveMicrowaveComponent>(uid); //microwave is now cooking
            activeComp.CookTimeRemaining = component.CurrentCookTimerTime * component.CookTimeMultiplier;
            activeComp.TotalTime = component.CurrentCookTimerTime; //this doesn't scale so that we can have the "actual" time
            activeComp.PortionedRecipe = portionedRecipe;
            //Scale tiems with cook times
            component.CurrentCookTimeEnd = _gameTiming.CurTime + TimeSpan.FromSeconds(component.CurrentCookTimerTime * component.CookTimeMultiplier);
            if (malfunctioning)
                activeComp.MalfunctionTime = _gameTiming.CurTime + TimeSpan.FromSeconds(component.MalfunctionInterval);
            UpdateUserInterfaceState(uid, component);
        }

        private void StopWashing(Entity<WashingMachineComponent> ent)
        {
            RemCompDeferred<ActiveWashingMachineComponent>(ent);
            foreach (var item in ent.Comp.Contents.ContainedEntities) // crate
            {
                RemCompDeferred<ActivelyWashedComponent>(item);
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<ActiveWashingMachineComponent, WashingMachineComponent>();
            while (query.MoveNext(out var uid, out var active, out var washingMachine))
            {

                active.WashTimeRemaining -= frameTime;

                RollMalfunction((uid, active, washingMachine)); //?

                //check if there's still wash time left
                if (active.WashTimeRemaining > 0)
                {
                    AddTemperature(washingMachine, frameTime);
                    continue;
                }

                //this means the cycle has finished.
                AddTemperature(washingMachine, Math.Max(frameTime + active.WashTimeRemaining, 0)); //Though there's still a little bit more heat to pump out

                if (active.PortionedRecipe.Item1 != null) // do crayon recipes
                {
                    var coords = Transform(uid).Coordinates;
                    for (var i = 0; i < active.PortionedRecipe.Item2; i++)
                    {
                        SubtractContents(microwave, active.PortionedRecipe.Item1);
                        Spawn(active.PortionedRecipe.Item1.Result, coords);
                        //remove 200u from solution
                    }
                }

                _container.EmptyContainer(washingMachine.Contents);
                washingMachine.CurrentCookTimeEnd = TimeSpan.Zero;
                _audio.PlayPvs(washingMachine.CycleDoneSound, uid);
                StopWashing((uid, washingMachine));
            }
        }

        /// <summary>
        /// This event tries to get secret recipes that the microwave might be capable of.
        /// Currently, we only check the microwave itself, but in the future, the user might be able to learn recipes.
        /// </summary>
        private void OnGetSecretRecipes(Entity<FoodRecipeProviderComponent> ent, ref GetSecretRecipesEvent args)
        {
            foreach (ProtoId<FoodRecipePrototype> recipeId in ent.Comp.ProvidedRecipes)
            {
                if (_prototype.TryIndex(recipeId, out var recipeProto))
                {
                    args.Recipes.Add(recipeProto);
                }
            }
        }
    }
}
