using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Impstation.Genetics.Events;

[ByRefEvent]
public readonly record struct GeneAddedEvent(EntityUid Performer);

[ByRefEvent]
public readonly record struct GeneRemovedEvent(EntityUid Performer);
