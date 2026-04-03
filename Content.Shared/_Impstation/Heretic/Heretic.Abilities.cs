using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared.Heretic;

#region Abilities

//Hunt
public sealed partial class EventHereticPlaceWatchtower : InstantActionEvent { }
public sealed partial class EventHereticTeachSerpentFocus : InstantActionEvent { }
public sealed partial class EventHereticSerpentFocus : InstantActionEvent { }
[Serializable, NetSerializable, DataDefinition] public sealed partial class EventHereticHuntAscend : EntityEventArgs { }

#endregion
