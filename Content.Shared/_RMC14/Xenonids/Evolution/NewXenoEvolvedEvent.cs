using Content.Shared._RMC14.Xenonids.Plasma;

namespace Content.Shared._RMC14.Xenonids.Evolution;

[ByRefEvent]
public readonly record struct NewXenoEvolvedEvent(Entity<XenoPlasmaComponent> OldXeno, EntityUid NewXeno, bool SubtractPoints);
