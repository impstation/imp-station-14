using Content.Shared.Radiation.Components;

namespace Content.Shared.Radiation.Systems;

public abstract partial class SharedRadiationSystem : EntitySystem
{
    [Dependency] protected readonly EntityQuery<RadiationSourceComponent> SourceQuery = default!;

    /// <summary>
    /// Sets the intensity of a <see cref="RadiationSourceComponent"/> to the passed intensity.
    /// </summary>
    /// <param name="entity">Radiation source we're attempting to update</param>
    /// <param name="slope">Intensity we're setting the source to.</param>
    public void SetIntensity(Entity<RadiationSourceComponent?> entity, float intensity)
    {
        if (!SourceQuery.Resolve(entity, ref entity.Comp, false))
            return;

        entity.Comp.Intensity = intensity;
        UpdateSource((entity, entity.Comp));
    }

    // IMP ADD
    /// <summary>
    /// Sets the slope of a <see cref="RadiationSourceComponent"/> to the passed slope.
    /// </summary>
    /// <param name="entity">Radiation source we're attempting to update</param>
    /// <param name="slope">Slope we're setting the source to.</param>
    public void SetSlope(Entity<RadiationSourceComponent?> entity, float slope)
    {
        if (!SourceQuery.Resolve(entity, ref entity.Comp, false))
            return;

        entity.Comp.Slope = slope;
        UpdateSource((entity, entity.Comp));
    }

    /// <summary>
    /// Updates the radiation source cache. Does nothing on client, see server!
    /// </summary>
    protected virtual void UpdateSource(Entity<RadiationSourceComponent> entity) { }
}
