using Content.Shared.Atmos.Components;
using Content.Shared.Atmos; // Imp addition due to move to _Funkystation

namespace Content.Shared._Funkystation.Atmos;

public interface IPipeNode
{
    PipeDirection Direction { get; }
    AtmosPipeLayer Layer { get; }
}
