using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.PowerCell;
using Content.Shared._Impstation.Homunculi.Components;
using Content.Shared._Impstation.Homunculi.Incubator;
using Content.Shared._Impstation.Homunculi.Incubator.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Forensics.Components;
using Content.Shared.Humanoid;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Sprite;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server._Impstation.Homunculi.Incubator;

public sealed class IncubatorSystem : SharedIncubatorSystem
{
    [Dependency] private readonly ItemToggleSystem _toggle = null!;
    [Dependency] private readonly PowerCellSystem _cell = null!;
    [Dependency] private readonly IGameTiming _timing = null!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = null!;
    [Dependency] private readonly ItemSlotsSystem _slots = null!;
    [Dependency] private readonly PuddleSystem _puddle = null!;
    [Dependency] private readonly SharedAudioSystem _audio = null!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IncubatorComponent, ItemToggleActivateAttemptEvent>(OnActivateAttempt);
        SubscribeLocalEvent<IncubatorComponent, ItemToggleDeactivateAttemptEvent>(OnDeactivateAttempt);
        SubscribeLocalEvent<IncubatorComponent, ItemToggledEvent>(OnToggled);
        SubscribeLocalEvent<IncubatorComponent, ExaminedEvent>(OnExamine);

    }

    private void OnActivateAttempt(Entity<IncubatorComponent> ent, ref ItemToggleActivateAttemptEvent args)
    {
        string? popup = null;

        var beaker = _slots.GetItemOrNull(ent, ent.Comp.BeakerSlotId);
        if (beaker == null)
        {
            popup = Loc.GetString("incubator-no-beaker");
        }
        else if (!TryGetSolution(ent.Owner, out var solution))
        {
            popup = Loc.GetString("incubator-no-solution");
        }
        else if (!TryGetDnaData(solution, out var dnaData))
        {
            popup = Loc.GetString("incubator-no-dna");
        }
        else if (dnaData.Count > 1)
        {
            popup = Loc.GetString("incubator-too-much-dna");
        }
        else if (!_cell.TryGetBatteryFromSlot(ent, out var battery))
        {
            popup = Loc.GetString("incubator-no-cell");
        }
        else if (UsesRemaining(ent.Comp, battery) <= 0)
        {
            popup = Loc.GetString("incubator-insufficient-power");
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

    private bool TryGetSolution(Entity<IncubatorComponent?> ent,
        [NotNullWhen(true)] out Entity<SolutionComponent>? solution)
    {
        if (!Resolve(ent, ref ent.Comp))
        {
            solution = null;
            return false;
        }

        var container = _slots.GetItemOrNull(ent, ent.Comp.BeakerSlotId);
        if (container != null && _solution.TryGetFitsInDispenser(container.Value, out var foundSolution, out _))
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

            FinishIncubation(uid);
        }
    }

    private void OnToggled(Entity<IncubatorComponent> ent, ref ItemToggledEvent args)
    {
        if (args.Activated)
            ent.Comp.PlayingStream = _audio.PlayPvs(ent.Comp.LoopingSound, ent, AudioParams.Default.WithLoop(true).WithMaxDistance(5))?.Entity;
        else
            _audio.Stop(ent.Comp.PlayingStream);
    }

    public void FinishIncubation(Entity<IncubatorComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        // Spawn Homunculi
        if (!SpawnHomunculi(ent))
        {
            if (TryGetSolution(ent, out var solution))
            {
                _puddle.TrySpillAt(ent, solution.Value.Comp.Solution, out _ );
                _solution.RemoveAllSolution(solution.Value);
            }
        }

        ent.Comp.IncubationFinishTime = null;
        _cell.TryUseCharge(ent, ent.Comp.ChargeUse);
        _toggle.TryDeactivate(ent.Owner);
    }

    public bool SpawnHomunculi(Entity<IncubatorComponent?> ent)
    {
        if (!TryGetSolution(ent, out var solution))
            return false;

        // If there's no DNA data in the solution (shouldn't happen but whatever) return
        if (!TryGetDnaData(solution, out var dnaData))
            return false;

        // Save a copy of the solutions reagents, can't just use it straight up
        var reagentList = solution.Value.Comp.Solution.Contents.ToList();

        var query = EntityQueryEnumerator<HomunculiTypeComponent>();
        while (query.MoveNext(out var victim, out var homunculiTypeComponent))
        {
            if (!SatisfiesRecipe(homunculiTypeComponent, reagentList))
                continue;
            if (!TryComp<DnaComponent>(victim, out var dnaComponent))
                continue;
            if (dnaComponent.DNA != dnaData.First().DNA)
                continue;

            var savedSolutions = solution.Value.Comp.Solution.Contents.ToList();
            // Go through all the reagents in the saved solution, if the reagent matches one in the recipe, remove it
            // I have to check for reagent data because it needs to be specific or else it won't drain
            foreach (var (reagent, amount) in homunculiTypeComponent.Recipe)
            {
                var match = savedSolutions.FirstOrDefault(rq => rq.Reagent.Prototype == reagent);

                if (match.Reagent.Data != null)
                    _solution.RemoveReagent(solution.Value, reagent, amount, match.Reagent.Data);
                else
                    _solution.RemoveReagent(solution.Value, reagent, amount);
            }

            var transform = EntityManager.GetComponent<TransformComponent>(ent);
            var transformSystem = EntityManager.System<SharedTransformSystem>();

            var homunculi = EntityManager.SpawnEntity(homunculiTypeComponent.HomunculiType, transformSystem.GetMapCoordinates(ent, xform: transform));
            transformSystem.AttachToGridOrMap(ent);

            EnsureComp<DnaComponent>(homunculi, out var homunculiDnaComponent);
            homunculiDnaComponent.DNA = dnaComponent.DNA;

            SetHomunculusAppearance(victim,homunculi);
            return true;
        }
        return false;
    }

    private void SetHomunculusAppearance(EntityUid urist, EntityUid homunculi)
    {
        (Color skinColor, Color eyeColor)? colors;
        if (TryComp<HumanoidAppearanceComponent>(urist, out var appearanceComponent))
        {
            colors = GetHumanoidColors(appearanceComponent);
        }
        else
            return;

        if (!TryComp<RandomSpriteComponent>(homunculi, out var randomSprite))
            return;

        foreach (var entry in randomSprite.Selected)
        {
            var state = randomSprite.Selected[entry.Key];
            state.Color = entry.Key switch
            {
                "skinMap" => colors.Value.skinColor,
                "eyeMap" => colors.Value.eyeColor,
                _ => state.Color,
            };
            randomSprite.Selected[entry.Key] = state;
        }
        Dirty(urist, randomSprite);
    }

    private static (Color skinColor, Color eyeColor) GetHumanoidColors(HumanoidAppearanceComponent comp)
    {
        return (comp.SkinColor , comp.EyeColor);
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
