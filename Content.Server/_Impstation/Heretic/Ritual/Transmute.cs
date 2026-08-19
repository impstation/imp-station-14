using Content.Server.Heretic.EntitySystems;
using Content.Shared._Impstation.Heretic.Components;
using Content.Shared._Impstation.Heretic.Ritual;
using Content.Shared.Heretic.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Heretic.Ritual;

public sealed partial class Transmute : EntitySystem, IRitualBehavior
{
    [Dependency] private readonly HereticRitualSystem _ritual = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public bool DoRitual(EntityUid performer, EntityUid platform, ProtoId<HereticRitualPrototype> ritualId)
    {
        return true;
    }
    public void DoRitualEffect(EntityUid performer, EntityUid platform, ProtoId<HereticRitualPrototype> ritualId)
    {
        var output = _ritual.GetRitual(ritualId).OutputItems ?? new Dictionary<EntProtoId, int>();
        foreach (var ent in output.Keys)
        {
            for (var i = 0; i < output[ent]; i++)
            {
                var spawn = Spawn(ent, Transform(platform).Coordinates);
                if (TryComp<MinionComponent>(spawn, out var minion))
                {
                    minion.BoundOwner = performer;
                }
            }
        }
    }
}
