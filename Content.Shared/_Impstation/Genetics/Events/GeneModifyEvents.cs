using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Impstation.Genetics.Events;

/// <summary>
/// Raised by the GeneSystem whenever a Gene is added to an Entity
/// </summary>
/// <param name="Performer"></param>
[ByRefEvent]
public readonly record struct GeneAddedEvent(EntityUid Performer);

/// <summary>
/// Raised by the GeneSystem whenever a Gene is removed from an Entity
/// </summary>
/// <param name="Performer"></param>
[ByRefEvent]
public readonly record struct GeneRemovedEvent(EntityUid Performer);
