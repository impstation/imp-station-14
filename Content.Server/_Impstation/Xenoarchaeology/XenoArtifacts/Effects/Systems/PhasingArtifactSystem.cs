using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Physics;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

/// <summary>
///     Handles allowing activated artifacts to phase through walls.
/// </summary>
public sealed class PhasingArtifactSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PhasingArtifactComponent, ArtifactActivatedEvent>(OnActivate);
    }

    private void OnActivate(Entity<PhasingArtifactComponent> ent, ref ArtifactActivatedEvent args)
    {
        if (!TryComp<FixturesComponent>(ent, out var fixtures))
            return;

        foreach (var fixture in fixtures.Fixtures.Values)
        {
            _physics.SetHard(ent, fixture, false, fixtures);
        }
    }
}