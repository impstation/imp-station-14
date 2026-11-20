using Content.Shared._Impstation.StatusEffectNew.Components;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.StatusEffectNew;
/// <summary>
/// System to handle adding and removing a faction along with the status effect.
/// </summary>
public sealed class FactionChangeSystem : EntitySystem
{
    [Dependency] private readonly NpcFactionSystem _faction = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FactionChangeStatusEffectComponent, StatusEffectAppliedEvent>(AddStatusFaction);
        SubscribeLocalEvent<FactionChangeStatusEffectComponent, StatusEffectRemovedEvent>(RemoveStatusFaction);
    }

    /// <summary>
    /// Remove the entity from the faction, unless they were already in it.
    /// </summary>
    private void RemoveStatusFaction(Entity<FactionChangeStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (ent.Comp.AlreadyIn)
        {
            return;
        }
        if (!TryComp<NpcFactionMemberComponent>(args.Target, out var npcFactionMember))
            return;
        var factionEnt = (args.Target, npcFactionMember);

        _faction.RemoveFaction(factionEnt,ent.Comp.Faction);
        Dirty(ent);
    }

    /// <summary>
    /// Remove the entity from the faction, unless they were already in it.
    /// </summary>
    private void AddStatusFaction(Entity<FactionChangeStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (!TryComp<NpcFactionMemberComponent>(args.Target, out var npcFactionMember))
            return;
        var factionEnt = (args.Target, npcFactionMember);

        if (_faction.IsMember(factionEnt, ent.Comp.Faction))
        {
            ent.Comp.AlreadyIn = true;
            return;
        }

        _faction.AddFaction(factionEnt, ent.Comp.Faction);
        Dirty(ent);
    }
}
