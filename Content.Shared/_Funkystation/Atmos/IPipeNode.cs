using Content.Shared.Atmos.Components;

namespace Content.Shared.Atmos;

public interface IPipeNode
{
    PipeDirection Direction { get; }
    AtmosPipeLayer Layer { get; }
}
