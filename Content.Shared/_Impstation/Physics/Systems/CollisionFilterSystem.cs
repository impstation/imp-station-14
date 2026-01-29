using System.Linq;
using Content.Shared._Impstation.Physics.Components;
using Content.Shared.Projectiles;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Physics.Systems;

/// <summary>
/// Used to filter collisions, AKA prevents them and lets entities pass through each other.
/// This shit wounded me so if you complain about it being bad I will know and I will find you.
/// </summary>
public sealed class CollisionFilterSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

    private EntityQuery<ProjectileComponent> _projectileQuery;
    private EntityQuery<ThrownItemComponent> _thrownQuery;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CollisionFilterComponent, PreventCollideEvent>(OnPreventCollide);

        _projectileQuery = GetEntityQuery<ProjectileComponent>();
        _thrownQuery = GetEntityQuery<ThrownItemComponent>();
    }

    private void OnPreventCollide(Entity<CollisionFilterComponent> ent, ref PreventCollideEvent args)
    {
        if (args.Cancelled)
            return;

        args.Cancelled = CheckForPointers(args.OtherEntity, ent.Comp);
    }

    /// <summary>
    /// Where the filtering happens.
    /// </summary>
    private bool CheckRequirements(EntityUid entity, CollisionFilterComponent filter)
    {
        if (!entity.IsValid())
            return false;

        // Check component requirement.
        if (!string.IsNullOrEmpty(filter.RequiredComponent))
        {
            var componentType = _componentFactory.GetRegistration(filter.RequiredComponent).Type;
            if (!HasComp(entity, componentType))
                return false;
        }

        // Check tags requirement.
        if (filter.RequiredTags != null && filter.RequiredTags.Count > 0)
        {
            if (filter.TagCheckMode == TagCheckMode.All)
            {
                // If all tags are required.
                if (!filter.RequiredTags.All(tag => _tagSystem.HasTag(entity, tag)))
                    return false;
            }
            else
            {
                // If only one tag is required.
                if (!filter.RequiredTags.Any(tag => _tagSystem.HasTag(entity, tag)))
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Runs an extra check on prototypes with certain components to see if they point towards other entities.
    /// This is what enables the filter to apply to entities that originate from an entity that has the required component/tags.
    /// Example: CollisionFilter is set to check for a specific gun.
    /// This function allows it to filter the projectiles fired by that gun but not by any other gun that shares the same ammo.
    /// </summary>
    private bool CheckForPointers(EntityUid otherEntity, CollisionFilterComponent filter)
    {
        var originChain = FollowPointers(otherEntity);
        var hasRequirement = originChain.Any(entity => CheckRequirements(entity, filter));

        return filter.FilterAll != hasRequirement;
    }

    /// <summary>
    /// Follows the relationships of entities so that it can apply the filter.
    /// Yes, it's hardcoded. Robust really hated all of my attempts to make it YAML configurable.
    /// Adding new pointers is simple though (unless it's a raycast).
    /// </summary>
    private List<EntityUid> FollowPointers(EntityUid otherEntity)
    {
        var chain = new List<EntityUid> { otherEntity };

        // Checks projectiles, hitscan, and guns.
        if (_projectileQuery.TryGetComponent(otherEntity, out var projectile))
        {
            if (projectile.Shooter.HasValue && projectile.Shooter.Value.IsValid())
                chain.Add(projectile.Shooter.Value);
            if (projectile.Weapon.HasValue && projectile.Weapon.Value.IsValid())
                chain.Add(projectile.Weapon.Value);
        }

        // Checks thrown objects.
        if (_thrownQuery.TryGetComponent(otherEntity, out var thrown))
        {
            if (thrown.Thrower.HasValue && thrown.Thrower.Value.IsValid())
                chain.Add(thrown.Thrower.Value);
        }

        return chain;
    }

    /// <summary>
    /// Nessecary for hitscan filtering to work properly. Gets passed through HitscanBasicRaycastSystem.
    /// Filtering all raycasts is technically doable but would require a lot more upstream edits.
    /// This will cover most things you'd want to filter anyways.
    /// </summary>
    public bool HitscanCollisionCheck(EntityUid origin, EntityUid target)
    {
        if (!TryComp<CollisionFilterComponent>(target, out var filter))
            return true;

        return !CheckForPointers(origin, filter);
    }
}
