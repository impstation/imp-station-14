using Content.Shared._Impstation.Heretic.Ritual;
using Content.Shared.Heretic.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Client.Heretic;

[Virtual]
public partial class Sacrifice : IRitualBehavior
{
    public bool DoRitual(EntityUid performer, EntityUid platform, ProtoId<HereticRitualPrototype> ritualId)
    {
        return true;
    }

    public void DoRitualEffect(EntityUid performer, EntityUid platform, ProtoId<HereticRitualPrototype> ritualId)
    {
        // Do nothing!
    }
}

public sealed partial class AshAscend : Sacrifice { }
public sealed partial class HuntAscend : Sacrifice { }
public sealed partial class MuteGhoulify : Sacrifice { }

public sealed partial class Temperature : IRitualBehavior
{
    public bool DoRitual(EntityUid performer, EntityUid platform, ProtoId<HereticRitualPrototype> ritualId)
    {
        return true;
    }

    public void DoRitualEffect(EntityUid performer, EntityUid platform, ProtoId<HereticRitualPrototype> ritualId)
    {
        // Do nothing!
    }
}

public sealed partial class ReagentPuddle : IRitualBehavior
{
    public bool DoRitual(EntityUid performer, EntityUid platform, ProtoId<HereticRitualPrototype> ritualId)
    {
        return true;
    }

    public void DoRitualEffect(EntityUid performer, EntityUid platform, ProtoId<HereticRitualPrototype> ritualId)
    {
        // Do nothing!
    }
}
public sealed partial class Transmute : IRitualBehavior
{
    public bool DoRitual(EntityUid performer, EntityUid platform, ProtoId<HereticRitualPrototype> ritualId)
    {
        return true;
    }

    public void DoRitualEffect(EntityUid performer, EntityUid platform, ProtoId<HereticRitualPrototype> ritualId)
    {
        // Do nothing!
    }
}
