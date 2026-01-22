using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.DoAfter;

namespace Content.Shared._Impstation.AnimalHusbandry.Components;

/// <summary>
/// Exists purely for the ability for Incubators to keep track of what they're doing
/// </summary>
[RegisterComponent]
public sealed partial class IncubatorComponent : Component
{
    // When do we finish incubation?
    public TimeSpan FinishIncubation = TimeSpan.Zero;

    // Egg we are currently incubating
    public IncubationComponent CurrentlyIncubated;
}

[Serializable]
public enum IncubatorVisualizerLayers : byte
{
    Status
}

[Serializable]
public enum IncubatorStatus : byte
{
    Active,
    Inactive
}


[ByRefEvent]
public record struct IncubatingAttemptEvent(EntityUid incubated, bool cancelled = false);
public readonly record struct AfterIncubationEvent();
public sealed partial class IncubationDoAfterEvent : SimpleDoAfterEvent { }
