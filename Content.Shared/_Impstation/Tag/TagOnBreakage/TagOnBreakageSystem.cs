using Robust.Shared.Prototypes;
using Content.Shared.Repairable;
using Content.Shared.Tag;
using Content.Shared.Destructible;
using System.Linq;

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
        SubscribeLocalEvent<TagOnBreakageComponent, SharedRepairableSystem>(OnRepaired);
    }
    public void OnBreak(Entity<TagOnBreakageComponent> entity, ref BreakageEventArgs args)
    {
        EnsureComp<TagComponent>(entity.Owner);
        AddTag(entity, entity.Comp.Tags);
    }

    public void OnRepaired(Entity<TagOnBreakageComponent> entity, ref SharedRepairableSystem args)
    {
        EnsureComp<TagComponent>(entity.Owner);
        RemoveTag(entity, entity.Comp.Tags);
    }

    public void AddTag(Entity<TagOnBreakageComponent> entity, ProtoId<TagPrototype>[] tags)
    {
        // Get the current tags from TagComponent (if any) and store them temporarily.
        if (!EntityManager.TryGetComponent(entity.Owner, out TagComponent? tagComp))
            return;
        // Remove the old tags if we're replacing them.
        if (entity.Comp.ReplaceTags == true)
        {
            _tagSystem.RemoveTags(entity.Owner, [.. tagComp.Tags]);
        }
        // If the ReTagOnRepair is set to true, we need to store the old tags for later.
        if (entity.Comp.ReTagOnRepair == true)
        {
            entity.Comp.OldTags = [.. tagComp.Tags];
        }
        // Add the breakage tags.
        _tagSystem.AddTags(entity.Owner, tags);
        entity.Comp.IsTagged = true;
    }

    public void RemoveTag(Entity<TagOnBreakageComponent> entity, ProtoId<TagPrototype>[] tags)
    {
        _tagSystem.RemoveTags(entity.Owner, tags);
        entity.Comp.IsTagged = false;
        // Add the original tags back if ReplaceTags and ReTagOnRepair is set to true.
        if (entity.Comp.ReplaceTags == true && entity.Comp.ReTagOnRepair == true)
        {
            _tagSystem.AddTags(entity.Owner, entity.Comp.OldTags);
            entity.Comp.OldTags = [];
        }
    }
}
