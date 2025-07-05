using Content.Server.Emp;
using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Robust.Server.GameObjects;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

public sealed class EmpArtifactSystem : EntitySystem
{
    [Dependency] private readonly EmpSystem _emp = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<EmpArtifactComponent, ArtifactActivatedEvent>(OnActivate);
    }

    private void OnActivate(Entity<EmpArtifactComponent> ent, ref ArtifactActivatedEvent args)
    {
        _emp.EmpPulse(_transform.GetMapCoordinates(ent), ent.Comp.Range, ent.Comp.EnergyConsumption, ent.Comp.DisableDuration);
    }
}