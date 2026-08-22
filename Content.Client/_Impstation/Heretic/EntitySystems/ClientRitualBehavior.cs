using Content.Shared._Impstation.Heretic.Ritual;
using Content.Shared.Heretic.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Client.Heretic;

[Virtual]
[DataDefinition]
public partial class SacrificeBehavior : SharedRitualBehaviorSystem
{
    public override bool DoRitual(EntityUid performer, EntityUid platform, ProtoId<HereticRitualPrototype> ritualId)
    {
        return true;
    }

    public override void DoRitualEffect(EntityUid performer, EntityUid platform, ProtoId<HereticRitualPrototype> ritualId)
    {
        // Do nothing!
    }
}

[DataDefinition]
public sealed partial class AshAscendBehavior : SacrificeBehavior { }

[DataDefinition]
public sealed partial class HuntAscendBehavior : SacrificeBehavior { }

[DataDefinition]
public sealed partial class MuteGhoulifyBehavior : SacrificeBehavior { }

[DataDefinition]
public sealed partial class TemperatureBehavior : SharedRitualBehaviorSystem
{
    public override bool DoRitual(EntityUid performer, EntityUid platform, ProtoId<HereticRitualPrototype> ritualId)
    {
        return true;
    }

    public override void DoRitualEffect(EntityUid performer, EntityUid platform, ProtoId<HereticRitualPrototype> ritualId)
    {
        // Do nothing!
    }
}

[DataDefinition]
public sealed partial class ReagentPuddleBehavior : SharedRitualBehaviorSystem
{
    public override bool DoRitual(EntityUid performer, EntityUid platform, ProtoId<HereticRitualPrototype> ritualId)
    {
        return true;
    }

    public override void DoRitualEffect(EntityUid performer, EntityUid platform, ProtoId<HereticRitualPrototype> ritualId)
    {
        // Do nothing!
    }
}

[DataDefinition]
public sealed partial class TransmuteBehavior : SharedRitualBehaviorSystem
{
    public override bool DoRitual(EntityUid performer, EntityUid platform, ProtoId<HereticRitualPrototype> ritualId)
    {
        return true;
    }

    public override void DoRitualEffect(EntityUid performer, EntityUid platform, ProtoId<HereticRitualPrototype> ritualId)
    {
        // Do nothing!
    }
}
