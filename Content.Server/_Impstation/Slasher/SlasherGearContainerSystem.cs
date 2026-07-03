using Content.Server._Impstation.Slasher.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item;
using Content.Shared.Mind;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Thief;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Slasher;

/// <summary>
/// Slasher-specific fork of the thief backpack selector.
/// This keeps the generic thief system untouched while allowing Slasher ownership tagging
/// and cosmetic-variant tracking during approval.
/// </summary>
public sealed class SlasherGearContainerSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    /// <summary>
    /// Subscribes local events and prepares dependencies for this system.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlasherGearContainerComponent, BoundUIOpenedEvent>(OnUIOpened);
        SubscribeLocalEvent<SlasherGearContainerComponent, ThiefBackpackApproveMessage>(OnApprove);
        SubscribeLocalEvent<SlasherGearContainerComponent, ThiefBackpackChangeSetMessage>(OnChangeSet);
    }

    /// <summary>
    /// Refreshes the gear-selection UI when a player opens the container.
    /// </summary>
    /// <param name="container">Container entity and component data.</param>
    /// <param name="args">UI opened event data.</param>
    private void OnUIOpened(Entity<SlasherGearContainerComponent> container, ref BoundUIOpenedEvent args)
    {
        UpdateUI(container.Owner, container.Comp);
    }

    /// <summary>
    /// Spawns the selected cosmetic set contents and finalizes ownership tracking when the player confirms.
    /// </summary>
    /// <param name="container">Container entity and component data.</param>
    /// <param name="args">Approval message from the UI.</param>
    private void OnApprove(Entity<SlasherGearContainerComponent> container, ref ThiefBackpackApproveMessage args)
    {
        if (container.Comp.SelectedSets.Count != container.Comp.MaxSelectedSets)
            return;

        EntityUid? ownerMind = _mind.TryGetMind(args.Actor, out var mindId, out _) ? mindId : null;
        var spawnedStorage = SpawnStorage(container);
        var variantGearId = ResolveVariantGear(container.Comp);

        foreach (var selection in container.Comp.SelectedSets)
        {
            var setId = container.Comp.PossibleSets[selection];
            SpawnSetContents(container.Owner, setId, spawnedStorage, ownerMind);
        }

        FinalizeStorage(args.Actor, spawnedStorage, ownerMind);
        SetSelectedVariant(args.Actor, variantGearId);

        _audio.PlayPvs(container.Comp.ApproveSound, Transform(container.Owner).Coordinates);
        QueueDel(container);
    }

    /// <summary>
    /// Spawns the optional storage entity used to receive approved cosmetic items.
    /// </summary>
    /// <param name="container">Container entity and component data.</param>
    private EntityUid? SpawnStorage(Entity<SlasherGearContainerComponent> container)
    {
        if (container.Comp.SpawnedStoragePrototype == null)
            return null;

        return Spawn(container.Comp.SpawnedStoragePrototype, _transform.GetMapCoordinates(container.Owner));
    }

    /// <summary>
    /// Resolves which starting-gear variant corresponds to the player's selected set.
    /// </summary>
    /// <param name="component">Container component containing selected set indices.</param>
    private static string? ResolveVariantGear(SlasherGearContainerComponent component)
    {
        foreach (var selection in component.SelectedSets)
        {
            var setId = component.PossibleSets[selection];
            if (component.SetToVariantGear.TryGetValue(setId, out var variantProtoId))
                return variantProtoId;
        }

        return null;
    }

    /// <summary>
    /// Spawns every item in the selected set and routes each spawned entity through ownership tagging
    /// and placement logic (into spawned storage when present, otherwise dropped next to the container).
    /// </summary>
    /// <param name="containerUid">Gear-container entity used as the spawn position anchor.</param>
    /// <param name="setId">Prototype ID of the thief-backpack set to materialize.</param>
    /// <param name="spawnedStorage">Optional storage entity that receives spawned item entities.</param>
    /// <param name="ownerMind">Optional owner mind used for Slasher appearance-ownership tagging.</param>
    private void SpawnSetContents(EntityUid containerUid,
        ProtoId<ThiefBackpackSetPrototype> setId,
        EntityUid? spawnedStorage,
        EntityUid? ownerMind)
    {
        var set = _proto.Index(setId);
        foreach (var item in set.Content)
        {
            var ent = Spawn(item, _transform.GetMapCoordinates(containerUid));
            TagAppearanceItem(ent, ownerMind);
            PlaceSpawnedItem(ent, containerUid, spawnedStorage);
        }
    }

    /// <summary>
    /// Marks a spawned cosmetic item as belonging to the approving slasher.
    /// </summary>
    /// <param name="entity">Spawned item entity.</param>
    /// <param name="ownerMind">Owner mind to associate with the item.</param>
    private void TagAppearanceItem(EntityUid entity, EntityUid? ownerMind)
    {
        if (ownerMind == null)
            return;

        var ownership = EnsureComp<SlasherItemOwnershipComponent>(entity);
        ownership.OwnerMind = ownerMind;
        ownership.Source = SlasherOwnedItemSource.AppearanceLoadout;
    }

    /// <summary>
    /// Places a spawned item into the approved storage entity or drops it next to the container.
    /// </summary>
    /// <param name="entity">Spawned item entity.</param>
    /// <param name="containerUid">Container used as the fallback drop anchor.</param>
    /// <param name="spawnedStorage">Optional storage entity receiving spawned items.</param>
    private void PlaceSpawnedItem(EntityUid entity, EntityUid containerUid, EntityUid? spawnedStorage)
    {
        if (!TryComp<ItemComponent>(entity, out _))
            return;

        if (spawnedStorage != null)
            _storage.Insert(spawnedStorage.Value, entity, out _, playSound: false);
        else
            _transform.DropNextTo(entity, containerUid);
    }

    /// <summary>
    /// Tags the spawned storage itself and hands it to the actor when one was created.
    /// </summary>
    /// <param name="actor">Entity performing this action.</param>
    /// <param name="spawnedStorage">Optional storage entity that received the chosen items.</param>
    /// <param name="ownerMind">Owner mind to associate with the storage entity.</param>
    private void FinalizeStorage(EntityUid actor, EntityUid? spawnedStorage, EntityUid? ownerMind)
    {
        if (spawnedStorage == null)
            return;

        TagAppearanceItem(spawnedStorage.Value, ownerMind);
        _hands.TryPickupAnyHand(actor, spawnedStorage.Value);
    }

    /// <summary>
    /// Stores the selected cosmetic variant on the actor for later death-maze loadout suppression.
    /// </summary>
    /// <param name="actor">Entity performing this action.</param>
    /// <param name="variantGearId">Starting-gear variant chosen by the player.</param>
    private void SetSelectedVariant(EntityUid actor, string? variantGearId)
    {
        if (variantGearId == null)
            return;

        var variantComp = EnsureComp<SlasherCosmeticVariantComponent>(actor);
        variantComp.SelectedVariantGearId = variantGearId;
    }

    /// <summary>
    /// Toggles a cosmetic set selection and refreshes the UI.
    /// </summary>
    /// <param name="container">Container entity and component data.</param>
    /// <param name="args">Set-change message from the UI.</param>
    private void OnChangeSet(Entity<SlasherGearContainerComponent> container, ref ThiefBackpackChangeSetMessage args)
    {
        if (!container.Comp.SelectedSets.Remove(args.SetNumber))
            container.Comp.SelectedSets.Add(args.SetNumber);

        UpdateUI(container.Owner, container.Comp);
    }

    /// <summary>
    /// Rebuilds and sends the current gear-selection UI state.
    /// </summary>
    /// <param name="uid">Gear container entity.</param>
    /// <param name="component">Resolved component state, if already available.</param>
    private void UpdateUI(EntityUid uid, SlasherGearContainerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var data = new Dictionary<int, ThiefBackpackSetInfo>();

        for (var i = 0; i < component.PossibleSets.Count; i++)
        {
            var set = _proto.Index(component.PossibleSets[i]);
            var selected = component.SelectedSets.Contains(i);
            data.Add(i, new ThiefBackpackSetInfo(set.Name, set.Description, set.Sprite, selected));
        }

        _ui.SetUiState(uid,
            ThiefBackpackUIKey.Key,
            new ThiefBackpackBoundUserInterfaceState(data, component.MaxSelectedSets, component.ToolName, component.ToolDesc));
    }
}
