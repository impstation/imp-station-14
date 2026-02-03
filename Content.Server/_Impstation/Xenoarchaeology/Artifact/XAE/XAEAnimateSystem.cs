using Content.Server.Revenant.EntitySystems;
using Content.Shared.Item;
using System.Linq;
using Robust.Shared.Random;
using Robust.Shared.Containers;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;
using Content.Server.Xenoarchaeology.Artifact.XAE.Components;

namespace Content.Server.Xenoarchaeology.Artifact.XAE;

/// <summary>
/// System for xeno artifact activation effect that is violently animating items in a certain range.
/// </summary>
public sealed class XAEAnimateSystem : BaseXAESystem<XAEAnimateComponent>
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RevenantAnimatedSystem _revenantAnimated = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAEAnimateComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
         // Get a list of all nearby objects in range

        var entsHash = _lookup.GetEntitiesInRange(args.Coordinates, ent.Comp.Range);
        entsHash.Add(args.Artifact.Owner);
        var numSuccessfulAnimates = 0;

        var unshuffledEnts = entsHash.ToList();
        var targets = unshuffledEnts.OrderBy(_ => _random.Next()).ToList();

        foreach (var target in targets)
        {
            if (numSuccessfulAnimates >= ent.Comp.Count)
                return;

            // need to only get items not in a container
            if (HasComp<ItemComponent>(target) && _revenantAnimated.CanAnimateObject(target) && !_container.IsEntityInContainer(target))
            {
                if (_revenantAnimated.TryAnimateObject(target, TimeSpan.FromSeconds(ent.Comp.Duration)))
                {
                    numSuccessfulAnimates += 1;
                }
            }
        }
    }
}
