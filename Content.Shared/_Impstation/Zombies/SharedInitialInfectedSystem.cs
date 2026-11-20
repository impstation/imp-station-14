using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared.Zombies;

public abstract class SharedInitialInfectedSystem : EntitySystem
{
}

public sealed partial class MutationShopActionEvent : InstantActionEvent
{
}

[Serializable, NetSerializable, DataDefinition] public sealed partial class MutateLungeEvent : EntityEventArgs { }
[Serializable, NetSerializable, DataDefinition] public sealed partial class MutateUnstoppableEvent : EntityEventArgs { }

public sealed partial class UnstoppableActionEvent : InstantActionEvent
{
}
