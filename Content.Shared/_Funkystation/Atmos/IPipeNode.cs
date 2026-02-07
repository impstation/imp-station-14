using Content.Shared.Atmos.Components;
using Content.Shared.Atmos; // Imp

namespace Content.Shared._Funkystation.Atmos; // Imp, moved to _Funkystation

public interface IPipeNode
{
    PipeDirection Direction { get; }
    AtmosPipeLayer Layer { get; }
}
