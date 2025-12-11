using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;

namespace Content.Shared._Impstation.Traits.Assorted;

/// <summary>
/// This handles adding factions when using the hated by dogs trait.
/// </summary>
public sealed class HatedByDogsSystem : EntitySystem
{
    [Dependency] private readonly NpcFactionSystem _faction = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HatedByDogsComponent, ComponentStartup>(AddFaction);
    }

    private void AddFaction(EntityUid uid, HatedByDogsComponent component, ComponentStartup args)
    {
        EnsureComp<NpcFactionMemberComponent>(uid, out var factionComp);
        _faction.AddFaction((uid, factionComp), component.Faction);
    }
}
