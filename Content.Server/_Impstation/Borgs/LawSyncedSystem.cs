using Content.Server.Silicons.Laws;
using Content.Server.StationEvents.Events;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;

namespace Content.Server._Impstation.Borgs.LawSync;

/// <summary>
/// Handles synchronizing (and desynchronizing) silicon lawsets from the AI upload console.
/// </summary>
public sealed class LawSyncSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlot = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly SiliconLawSystem _siliconLaws = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LawSyncedComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<LawSyncedComponent, SiliconEmaggedEvent>(OnGotEmagged);
        SubscribeLocalEvent<LawSyncedComponent, IonStormEvent>(OnIonStormTargeted);
    }

    private void OnComponentStartup(Entity<LawSyncedComponent> ent, ref ComponentStartup args)
    {
        SyncLaws(ent);
    }

    public void SyncLaws(Entity<LawSyncedComponent> ent)
    {
        // build and test....
        if (!HasComp<SiliconLawProviderComponent>(ent))
            return;

        var curLawset = _siliconLaws.GetLaws(ent);
        // NOTE: This only really works if there's only one upload console with a lawboard in it at a time.
        // I can't think of a situation where there'd be more than one, but it's worth taking into account.
        // This could be fixed with a "SyncedTo" datafield in LawSynced & checking against that in
        // SiliconLawSystem.OnUpdaterInsert(), but I didn't want to change any upstream systems.
        var lawUpdaterQuery = EntityQueryEnumerator<SiliconLawUpdaterComponent>();

        while (lawUpdaterQuery.MoveNext(out var uid, out _))
        {
            // this whole mess just gets the LawProviderComponent out of the updater's slot item.
            if (!TryComp<ItemSlotsComponent>(uid, out var itemSlotsComp) ||
                !_itemSlot.TryGetSlot(uid, ent.Comp.TargetSlotId, out var maybeLawSlot, itemSlotsComp) ||
                maybeLawSlot is not { } lawSlot ||
                !lawSlot.HasItem ||
                !TryComp<SiliconLawProviderComponent>(lawSlot.Item, out var maybeLawProviderComp) ||
                maybeLawProviderComp is not { } lawProviderComp)
                continue;

            var lawset = lawProviderComp.Lawset ?? _siliconLaws.GetLawset(lawProviderComp.Laws);

            var lawsetIsNew = true;
            // compare the list of laws in each lawset. if they're the same, we don't need to change lawsets.
            if (lawset.Laws.Count != curLawset.Laws.Count)
                lawsetIsNew = true;
            else
                foreach (var law in lawset.Laws)
                {
                    foreach (var curLaw in curLawset.Laws)
                    {
                        if (law.LawString == curLaw.LawString)
                            lawsetIsNew = false;
                    }
                }

            if (lawsetIsNew)
                _siliconLaws.SetLaws(lawset.Laws, ent, lawProviderComp.LawUploadSound);
        }

        if (!_mind.TryGetMind(ent, out var mind, out _))
            return;
        // ensure that we remove any antagonist silicon roles if applicable.
        if (_role.MindHasRole<SubvertedSiliconRoleComponent>(mind))
            _role.MindRemoveRole<SubvertedSiliconRoleComponent>(mind);
    }

    /// <summary>
    /// Remove LawSyncedComponent when the entity is emagged.
    /// </summary>
    private void OnGotEmagged(Entity<LawSyncedComponent> ent, ref SiliconEmaggedEvent args)
    {
        RemCompDeferred<LawSyncedComponent>(ent);
    }

    /// <summary>
    /// Remove LawSyncedComponent when the entity is the target of an Ion Storm.
    /// </summary>
    private void OnIonStormTargeted(Entity<LawSyncedComponent> ent, ref IonStormEvent args)
    {
        RemCompDeferred<LawSyncedComponent>(ent);
    }
}
