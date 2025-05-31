using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared._RMC14.NPC;
using Content.Server._RMC14.NPC.HTN;

namespace Content.Server._RMC14.NPC;

public sealed class RMCNPCSystem : SharedRMCNPCSystem
{
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SleepNPCComponent, MapInitEvent>(OnSleepNPCMapInit, after: [typeof(HTNSystem)]);
    }

    private void OnSleepNPCMapInit(Entity<SleepNPCComponent> ent, ref MapInitEvent args)
    {
        SleepNPC(ent);
    }

    public override void SleepNPC(EntityUid id)
    {
        base.SleepNPC(id);
        _npc.SleepNPC(id);
    }

    public override void WakeNPC(EntityUid id)
    {
        base.WakeNPC(id);
        _npc.WakeNPC(id);
    }
}
