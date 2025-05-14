using System.Linq;
using Robust.Shared.Prototypes;
using Content.Shared.Tag;
using Content.Shared.Destructible;
using Content.Shared.Repairable;

namespace Content.Shared._Impstation.Tag.TagOnBreakage;

/// <summary>
/// System for the TagOnBreakageComponent.
/// </summary>
public sealed class TagOnBreakageSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tagSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TagOnBreakageComponent, BreakageEventArgs>(OnBreak);
        SubscribeLocalEvent<TagOnBreakageComponent, SharedRepairableSystem.RepairFinishedEvent>(OnRepaired);
    }

    /// <summary>
    /// Get all the tags from an entity,
    /// </summary>
    /// <returns>Returns all the assigned TagPrototypes from a TagComponent, if any.</returns>
    public ProtoId<TagPrototype>[]? TryGetTags(EntityUid uid)
    {
        EnsureComp<TagComponent>(uid, out var component);
        return component.Tags.ToArray();
    }

    private void OnBreak(Entity<TagOnBreakageComponent> entity, ref BreakageEventArgs args)
    {

        var currentTags = TryGetTags(entity.Owner);
        AddTag(entity, currentTags ?? []);
    }
    private void OnRepaired(Entity<TagOnBreakageComponent> entity, ref SharedRepairableSystem.RepairFinishedEvent args)
    {
        RemoveTag(entity);
    }

    private void AddTag(Entity<TagOnBreakageComponent> entity, ProtoId<TagPrototype>[] currentTags)
    {
        if (entity.Comp.IsTagged)
            return;
        // Remove the old tags if we're replacing them.
        if (entity.Comp.ReplaceTags)
        {
            _tagSystem.RemoveTags(entity.Owner, currentTags);
            // If ReTagOnRepair is set to true, we need to store the old tags for later.
            if (entity.Comp.ReTagOnRepair)
            {
                entity.Comp.OldTags = currentTags;
            }
        }
        // Add the breakage tags.
        _tagSystem.AddTags(entity.Owner, entity.Comp.Tags);
        entity.Comp.IsTagged = true;
    }

    private void RemoveTag(Entity<TagOnBreakageComponent> entity)
    {
        // If the entity isn't tagged, return to avoid an infinite loop. Why does an infinite loop happen without this? I don't know...
        if (!entity.Comp.IsTagged)
            return;

        _tagSystem.RemoveTags(entity.Owner, entity.Comp.Tags);
        entity.Comp.IsTagged = false;

        // Add the original tags back if ReplaceTags and ReTagOnRepair is set to true.
        if (entity.Comp.ReplaceTags && entity.Comp.ReTagOnRepair && entity.Comp.OldTags.Length > 0)
        {
            var oldTags = entity.Comp.OldTags; // Put the original tags in a temporary variable. Something something recursion.
            entity.Comp.OldTags = []; // Remove the storage of the original tags.
            _tagSystem.AddTags(entity.Owner, oldTags); // Add the original tags back.
        }
        else
        {
            entity.Comp.OldTags = [];
        }
    }
}
