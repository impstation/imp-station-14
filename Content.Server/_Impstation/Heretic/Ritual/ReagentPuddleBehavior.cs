using Content.Shared._Impstation.Heretic.Ritual;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Fluids.Components;
using Content.Shared.Heretic.Prototypes;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Server.Heretic.Ritual;

/// <summary>
/// Behavior for making a ritual require certain reagents (in puddle form).
/// </summary>
[DataDefinition]
public sealed partial class ReagentPuddleBehavior : SharedRitualBehaviorSystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    /// <summary>
    /// Whitelist for reagents that can be used for the given ritual.
    /// </summary>
    [DataField] public List<ProtoId<ReagentPrototype>>? Reagents { get; set; } = [];

    /// <summary>
    /// The puddles of whatever that are being checked.
    /// </summary>
    private List<EntityUid> _puddles = new();

    public override void Initialize()
    {
        base.Initialize();
    }

    public override bool DoRitual(EntityUid performer, EntityUid platform, ProtoId<HereticRitualPrototype> ritualId)
    {
        // We need reagents to not be null.
        if (Reagents == null)
        {
            _popup.PopupEntity(Loc.GetString("heretic-ritual-unknown"), platform, performer);
            return false;
        }

        // List of reagents that eventually gets pushed as a popup, if none of the whitelisted reagents are found.
        string reagStrings = "";

        foreach (var reagent in Reagents)
        {
            reagStrings += _proto.Index<ReagentPrototype>(reagent.Id).LocalizedName + ", ";

            var lookup = _lookup.GetEntitiesInRange(platform, .75f);

            foreach (var ent in lookup)
            {
                if (!TryComp<PuddleComponent>(ent, out var puddle))
                    continue;

                if (puddle.Solution == null)
                    continue;

                var soln = puddle.Solution.Value;

                if (!soln.Comp.Solution.ContainsPrototype(reagent))
                    continue;

                _puddles.Add(ent);
            }

            if (_puddles.Count == 0)
            {
                continue;
            }

            return true;
        }

        //take off the comma + space on the end of the reagStrings
        reagStrings = reagStrings.Substring(0, reagStrings.Length - 2);
        _popup.PopupEntity(Loc.GetString("heretic-ritual-fail-reagentpuddle", ("reagentname", reagStrings)), platform, performer);
        return false;
    }

    public override void DoRitualEffect(EntityUid performer, EntityUid platform, ProtoId<HereticRitualPrototype> ritualId)
    {
        // Delete all the puddles on ritual success.
        foreach (var uid in _puddles)
            QueueDel(uid);

        // Reset the puddleslist.
        _puddles = [];
    }
}
