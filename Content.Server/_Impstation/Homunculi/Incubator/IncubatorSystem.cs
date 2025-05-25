using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.PowerCell;
using Content.Shared._Impstation.Homunculi.Components;
using Content.Shared._Impstation.Homunculi.Incubator;
using Content.Shared._Impstation.Homunculi.Incubator.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Forensics.Components;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Robust.Shared.Timing;
using YamlDotNet.Serialization.Schemas;

namespace Content.Server._Impstation.Homunculi.Incubator;

public sealed class IncubatorSystem : SharedIncubatorSystem
{
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly PowerCellSystem _cell = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IncubatorComponent, ItemToggleActivateAttemptEvent>(OnActivateAttempt);
        SubscribeLocalEvent<IncubatorComponent, ItemToggleDeactivateAttemptEvent>(OnDeactivateAttempt);

        SubscribeLocalEvent<IncubatorComponent, ExaminedEvent>(OnExamine);

    }

    private void OnActivateAttempt(Entity<IncubatorComponent> ent, ref ItemToggleActivateAttemptEvent args)
    {
        string? popup = null;

        var beaker = _slots.GetItemOrNull(ent, ent.Comp.BeakerSlotId);
        if (beaker == null)
        {
            popup = "no beaker stupid";
        }
        else if (!TryGetSolutionFromBeaker(ent.Comp,(Entity<SolutionContainerManagerComponent?>)beaker, out var solution) ||
                 solution.Value.Comp.Solution.Contents.Count == 0)
        {
            popup = "no solution stupid";
        }
        else if (!TryGetDnaData(solution, out var dnaData))
        {
            popup = "no dna data stupid";
        }
        else if (dnaData.Count > 1)
        {
            popup = "too much dna data stupid";
        }
        else if (!_cell.TryGetBatteryFromSlot(ent, out var battery))
        {
            popup = "no battery stupid";
        }
        else if (UsesRemaining(ent.Comp, battery) <= 0)
        {
            popup = "no battery charge stupid";
        }

        if (popup != null)
        {
            args.Popup = popup;
            args.Cancelled = true;
            return;
        }

        ent.Comp.IncubationFinishTime = _timing.CurTime + ent.Comp.IncubationDuration;
    }

    private static void OnDeactivateAttempt(Entity<IncubatorComponent> ent, ref ItemToggleDeactivateAttemptEvent args)
    {
        if (ent.Comp.IncubationFinishTime != null)
        {
            args.Cancelled = true;
        }
    }

    private bool TryGetSolutionFromBeaker(IncubatorComponent incubator, Entity<SolutionContainerManagerComponent?> solutionManager, [NotNullWhen(true)] out Entity<SolutionComponent>? solution)
    {
        if (_solution.TryGetSolution(solutionManager, incubator.BeakerContainerId, out var foundSolution, out _))
        {
            solution = foundSolution;
            return true;
        }
        solution = null;
        return false;
    }

    private static bool TryGetDnaData(SolutionComponent solution, [NotNullWhen(true)] out List<DnaData>? dnaData)
    {
        dnaData = [];
        dnaData.AddRange(solution.Solution.Contents.SelectMany(reagent
            => reagent.Reagent.EnsureReagentData())
            .OfType<DnaData>());
        return dnaData.Count > 0;
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<IncubatorComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.IncubationFinishTime == null || comp.IncubationFinishTime > _timing.CurTime)
                continue;

            FinishIncubation(uid,comp);
        }
    }

    public void FinishIncubation(EntityUid uid, IncubatorComponent incubator)
    {

        // Spawn Homunculi
        if (!SpawnHomunculi(uid,incubator))
        {
            var beaker = _slots.GetItemOrNull(uid, incubator.BeakerSlotId);
            if (beaker != null && TryGetSolutionFromBeaker(incubator,(Entity<SolutionContainerManagerComponent?>)beaker, out var solution))
            {
                _puddle.TrySpillAt(uid, solution.Value.Comp.Solution, out _ );
                _solution.RemoveAllSolution(solution.Value);
            }
        }

        incubator.IncubationFinishTime = null;

        _cell.TryUseCharge(uid, incubator.ChargeUse);

        _toggle.TryDeactivate(uid);
    }

    public bool SpawnHomunculi(EntityUid uid, IncubatorComponent incubator)
    {
        // Get Beaker, if Beaker is null or contains no solution, return false
        var beaker = _slots.GetItemOrNull(uid, incubator.BeakerSlotId);
        if (beaker == null || !TryGetSolutionFromBeaker(incubator,(Entity<SolutionContainerManagerComponent?>)beaker, out var solution))
            return false;
        // If there's no DNA data in the solution (shouldn't happen but whatever) return
        if (!TryGetDnaData(solution, out var dnaData))
            return false;

        // Save a copy of the solutions reagents, can't just use it straight up
        var reagentList = solution.Value.Comp.Solution.Contents.ToList();

        var query = EntityQueryEnumerator<HomunculiTypeComponent>();
        while (query.MoveNext(out var victim, out var homunculiTypeComponent))
        {
            // If you cant make it you cant make it!!
            if (!SatisfiesRecipe(homunculiTypeComponent, reagentList))
                continue;
            // Check for DNA!
            if (!TryComp<DnaComponent>(victim, out var dnaComponent))
                continue;
            if (dnaComponent.DNA != dnaData.First().DNA)
                continue;

            var savedSolutions = solution.Value.Comp.Solution.Contents.ToList();
            // Go through all the reagents in the saved solution, if the reagent matches one in the recipe, remove it
            // I have to check for reagent data because it needs to be specific smh smh smh
            foreach (var (reagent, amount) in homunculiTypeComponent.Recipe)
            {
                var match = savedSolutions.FirstOrDefault(rq => rq.Reagent.Prototype == reagent);

                if (match.Reagent.Data != null)
                    _solution.RemoveReagent(solution.Value, reagent, amount, match.Reagent.Data);
                else
                    _solution.RemoveReagent(solution.Value, reagent, amount);
            }

            var transform = EntityManager.GetComponent<TransformComponent>(uid);
            var transformSystem = EntityManager.System<SharedTransformSystem>();

            // Spawn Homunculi Entity
            var homunculi = EntityManager.SpawnEntity(homunculiTypeComponent.HomunculiType, transformSystem.GetMapCoordinates(uid, xform: transform));
            transformSystem.AttachToGridOrMap(uid);

            // Copy DNA from Homunculi DNA donor to the Homunculi
            EnsureComp<DnaComponent>(homunculi, out var homunculiDnaComponent);
            homunculiDnaComponent.DNA = dnaComponent.DNA;

            return true;
        }
        return false;
    }

    private static bool SatisfiesRecipe(HomunculiTypeComponent component, List<ReagentQuantity> reagents)
    {
        foreach (var required in component.Recipe)
        {
            var available = reagents.FirstOrDefault(r => r.Reagent.Prototype == required.Key);

            if (available.Quantity < required.Value)
                return false;
        }

        return true;
    }


    private void OnExamine(Entity<IncubatorComponent> ent, ref ExaminedEvent args)
    {
        _cell.TryGetBatteryFromSlot(ent, out var battery);
        var charges = UsesRemaining(ent, battery);
        var maxCharges = MaxUses(ent, battery);

        using (args.PushGroup(nameof(IncubatorComponent)))
        {
            args.PushMarkup(Loc.GetString("limited-charges-charges-remaining", ("charges", charges)));

            if (charges > 0 && charges == maxCharges)
            {
                args.PushMarkup(Loc.GetString("limited-charges-max-charges"));
            }
        }
    }

    private static int UsesRemaining(IncubatorComponent component, BatteryComponent? battery = null)
    {
        if (battery == null || component.ChargeUse == 0f)
            return 0;

        return (int) (battery.CurrentCharge / component.ChargeUse);
    }

    private static int MaxUses(IncubatorComponent component, BatteryComponent? battery = null)
    {
        if (battery == null || component.ChargeUse == 0f)
            return 0;

        return (int) (battery.MaxCharge / component.ChargeUse);
    }

}
