using Content.Shared.Administration.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Administration.Components;

[NetworkedComponent]
public abstract partial class SharedEatSignComponent : Component;

[ByRefEvent]
public record struct EatSignAddedEvent;
