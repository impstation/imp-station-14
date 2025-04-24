// these are HEAVILY based on the Bingle free-agent ghostrole from GoobStation, but reflavored and reprogrammed to make them more Robust (and less of a meme.)
// all credit for the core gameplay concepts and a lot of the core functionality of the code goes to the folks over at Goob, but I re-wrote enough of it to justify putting it in our filestructure.
// the original Bingle PR can be found here: https://github.com/Goob-Station/Goob-Station/pull/1519

using Robust.Shared.Map;

namespace Content.Server._Impstation.Replicator;

public sealed class ReplicatorSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReplicatorComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ReplicatorComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.MyNest != null)
            return;

        var xform = Transform(ent);
        var coords = xform.Coordinates;

        if (!coords.IsValid(EntityManager) || xform.MapID == MapId.Nullspace)
            return;
        
        if (ent.Comp.Queen)
            ent.Comp.MyNest = Spawn("ReplicatorNest", xform.Coordinates);
        else // TODO: make this significantly less fuckstupid. this is GARBAGE code holy shit
        {
            var query = EntityQueryEnumerator<ReplicatorComponent>();
            while (query.MoveNext(out var queryUid, out var _))
            {
                if (xform.Coordinates == Transform(queryUid).Coordinates)
                    component.MyPit = queryUid;
            }
        }
    }
}
