using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.DoAfter;
using Content.Shared.Storage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Impstation.AnimalHusbandry.Components;

/// <summary>
/// Exists purely for the ability for Incubators to keep track of what they're doing
/// </summary>
[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentPause]
public sealed partial class IncubatorComponent : Component
{
    // When do we finish incubation?
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan FinishIncubation = TimeSpan.Zero;

    // Egg we are currently incubating
    public IncubationComponent CurrentlyIncubated;

    // Used for tracking visuals
    public IncubatorStatus Status;
}

[Serializable, NetSerializable]
public enum IncubatorVisualizerLayers : byte
{
    Status
}

[Serializable, NetSerializable]
public enum IncubatorStatus : byte
{
    Active,
    Inactive
}
