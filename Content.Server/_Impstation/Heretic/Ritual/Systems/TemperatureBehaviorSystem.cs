using Content.Server._Impstation.Heretic.Ritual;
using Content.Server.Atmos.EntitySystems;
using Content.Shared._Impstation.Heretic.Ritual;
using Content.Shared.Atmos;
using Content.Shared.Heretic.Prototypes;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Server.Heretic.Ritual;

/// <summary>
/// Behavior for making a ritual require certain temperature conditions.
/// </summary>
public sealed partial class TemperatureBehaviorSystem : SharedRitualBehaviorSystem
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public bool DoRitual(EntityUid performer, EntityUid platform, TemperatureBehaviorComponent comp)
    {
        // Get the gas mixture for the center tile of the platform.
        var mix = _atmos.GetTileMixture(platform);

        if (mix == null || mix.TotalMoles == 0) // just accept space as it is
            return true;

        if (mix.Temperature > Atmospherics.T0C + comp.MaxThreshold)
        {
            _popup.PopupEntity(Loc.GetString("heretic-ritual-fail-temperature-hot"), platform, performer);
            return false;
        }
        if (mix.Temperature > Atmospherics.T0C + comp.MinThreshold)
        {
            _popup.PopupEntity(Loc.GetString("heretic-ritual-fail-temperature-cold"), platform, performer);
            return false;
        }

        return true;
    }
}
