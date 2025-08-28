using Content.Server.Station.Systems;
using Content.Shared._Impstation.Service;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Service;

public sealed partial class ServiceJobSystem
{
    [Dependency] private readonly StationSystem _station = default!;

    private void OnJobClaim(Entity<ServiceJobBoardConsoleComponent> ent, ref ServiceJobClaimEvent args)
    {
        var station = _station.GetOwningStation(ent);

        if ()
    }
}
