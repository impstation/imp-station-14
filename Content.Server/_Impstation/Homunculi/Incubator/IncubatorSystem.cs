using Content.Server.Fluids.EntitySystems;
using Content.Server.PowerCell;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Power.Components;
using Content.Shared._Impstation.Homunculi.Incubator.Components;
using Content.Shared._Impstation.Homunculi.Incubator;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Server._Impstation.Homunculi.Incubator;

public sealed class IncubatorSystem : SharedIncubatorSystem
{
    [Dependency] private readonly HomunculusSystem _homunculus = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly PowerCellSystem _cell = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IncubatorComponent, ItemToggleActivateAttemptEvent>(OnActivateAttempt);
        SubscribeLocalEvent<ActiveIncubatorComponent, ItemToggleDeactivateAttemptEvent>(OnDeactivateAttempt);
        SubscribeLocalEvent<IncubatorComponent, ItemToggledEvent>(OnToggled);
        SubscribeLocalEvent<IncubatorComponent, ExaminedEvent>(OnExamine);

    }

    private void OnActivateAttempt(Entity<IncubatorComponent> ent, ref ItemToggleActivateAttemptEvent args)
    {
        string? popup = null;

        if (_slots.GetItemOrNull(ent, ent.Comp.BeakerSlotId) == null)
            popup = Loc.GetString("incubator-no-beaker");
        else if (!TryGetSolution(ent.Owner, out var solution))
            popup = Loc.GetString("incubator-no-solution");
        else if (!HasDnaData(solution))
            popup = Loc.GetString("incubator-no-dna");
        else if (!_cell.TryGetBatteryFromSlot(ent, out var battery))
            popup = Loc.GetString("incubator-no-cell");
        else if (UsesRemaining(ent.Comp, battery) <= 0)
            popup = Loc.GetString("incubator-insufficient-power");

        if (popup == null)
        {
            EnsureComp<ActiveIncubatorComponent>(ent, out var activeIncubator);
            activeIncubator.IncubationFinishTime = _timing.CurTime + ent.Comp.IncubationDuration;
        }
        else
        {
            args.Popup = popup;
            args.Cancelled = true;
        }
    }

    private void OnDeactivateAttempt(Entity<ActiveIncubatorComponent> ent, ref ItemToggleDeactivateAttemptEvent args)
    {
        if (ent.Comp.IncubationFinishTime == null)
            return;

        RemComp<ActiveIncubatorComponent>(ent);
        args.Cancelled = true;
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

    public bool HasDnaData(SolutionComponent solution)
    {
        List<DnaData> dnaData = [];
        dnaData.AddRange(solution.Solution.Contents.SelectMany(reagent
            => reagent.Reagent.EnsureReagentData())
            .OfType<DnaData>());
        return dnaData.Count > 0;
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveIncubatorComponent>();
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

    private void FinishIncubation(Entity<IncubatorComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        TryGetSolution(ent, out var solution);

        // Spawn Homunculi
        if (solution != null && !_homunculus.CreateHomunculiWithDna(ent,solution.Value,_transform.GetMapCoordinates(ent), out _))
        {
            _puddle.TrySpillAt(ent, solution.Value.Comp.Solution, out _ );
            _solution.RemoveAllSolution(solution.Value);
        }

        if (TryComp<ActiveIncubatorComponent>(ent, out var activeIncubator))
            activeIncubator.IncubationFinishTime = null;

        _cell.TryUseCharge(ent, ent.Comp.ChargeUse);
        _toggle.TryDeactivate(ent.Owner);
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
