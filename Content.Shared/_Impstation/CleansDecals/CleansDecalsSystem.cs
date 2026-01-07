using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Decals;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.CleansDecals;

public sealed class CleansDecalsSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDecalSystem _decals = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CleansDecalsComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<CleansDecalsComponent, CleanDecalsDoAfterEvent>(OnCleanDecalsDoAfter);
    }

    private void OnAfterInteract(EntityUid uid, CleansDecalsComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        TryStartCleaning(uid, component, args.User, args.ClickLocation);
    }

    public bool TryStartCleaning(EntityUid uid, CleansDecalsComponent component, EntityUid user, EntityCoordinates target)
    {
        if (!target.IsValid(EntityManager))
        {
            return false;
        }

        var grid = _transform.GetGrid(target);

        if (grid == null)
        {
            return false;
        }

        var decals = _decals.GetDecalsInRange(grid.Value, target.Position, component.CleanRadius);

        if (decals.Count == 0)
            return false;

        if (!_solutionContainer.TryGetSolution(uid, component.SolutionContainer, out var solution))
            return false;

        if (solution.Value.Comp.Solution.Volume < component.SolutionUsage)
        {
            _popup.PopupClient("You don't have enough soap!", user, PopupType.MediumCaution);
            return false;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, user, component.CleanDelay, new CleanDecalsDoAfterEvent(decals, grid.Value), uid, null, uid)
        {
            NeedHand = true,
            BreakOnDamage = true,
            BreakOnMove = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        _popup.PopupClient("You begin cleaning the floor...", user);
        return true;
    }

    private void OnCleanDecalsDoAfter(EntityUid uid, CleansDecalsComponent component, CleanDecalsDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (!_solutionContainer.TryGetSolution(uid, component.SolutionContainer, out var solution))
            return;

        _audio.PlayPredicted(component.CleanSound, uid, args.User);

        foreach (var decal in args.Decals)
        {
            if (solution.Value.Comp.Solution.Volume < component.SolutionUsage)
            {
                _popup.PopupClient("You've run out of soap!", args.User, PopupType.MediumCaution);
                break;
            }

            _solutionContainer.SplitSolution(solution.Value, component.SolutionUsage);
            _decals.RemoveDecal(args.Grid, decal.Item1);
        }
    }
}

[Serializable, NetSerializable]
public sealed partial class CleanDecalsDoAfterEvent : DoAfterEvent
{
    public HashSet<(uint, Decal)> Decals;

    [NonSerialized]
    public EntityUid Grid;

    public CleanDecalsDoAfterEvent(HashSet<(uint, Decal)> decals, EntityUid grid)
    {
        Decals = decals;
        Grid = grid;
    }

    public override DoAfterEvent Clone()
    {
        return this;
    }
}
