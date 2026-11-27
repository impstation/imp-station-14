using Content.Shared._Impstation.Heretic.Ritual;

namespace Content.Client.Heretic;

[Virtual]
public partial class SacrificeBehavior : SharedRitualBehaviorSystem { }

public sealed partial class AshAscendBehavior : SacrificeBehavior { }

public sealed partial class HuntAscendBehavior : SacrificeBehavior { }

public sealed partial class MuteGhoulifyBehavior : SacrificeBehavior { }

public sealed partial class TemperatureBehavior : SharedRitualBehaviorSystem { }

public sealed partial class ReagentPuddleBehavior : SharedRitualBehaviorSystem { }

public sealed partial class TransmuteBehavior : SharedRitualBehaviorSystem { }
