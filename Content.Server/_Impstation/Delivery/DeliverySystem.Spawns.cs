using Content.Server.Mind;
using Content.Server.Roles.Jobs;
using Content.Shared.Containers;
using Content.Shared.Delivery;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Roles;

namespace Content.Server.Delivery;

public sealed partial class DeliverySystem
{
    [Dependency] private readonly ContainerFillSystem _containerFill = default!;
    [Dependency] private readonly JobSystem _job = default!; // underwhelming gadget _job

    /// <summary>
    /// Fills a delivery entity with loot from the best applicable mail loot table.
    /// </summary>
    /// <remarks>
    /// This is called when the map initializes.
    /// </remarks>
    /// <param name="ent">The delivery entity to populate.</param>
    /// <param name="jobProto">A job prototype ID associated with the recipient.</param>
    private void PopulateContents(Entity<DeliveryComponent> ent, string jobProto)
    {
        var containerId = ent.Comp.Container;
        var table = GetDeliveryLootTable(ent, jobProto);

        _containerFill.FillContainer(ent.Owner, containerId, table, componentName: nameof(DeliveryComponent));
    }

    /// <summary>
    /// Gets the best applicable loot table for a given delivery entity.
    /// </summary>
    /// <remarks>
    /// This goes in descending order of priority: per-job mail, per-department mail, then generic mail.
    /// Primary departments (e.g. science, engineering) are prioritized over non-primary departments (command).
    /// </remarks>
    /// <param name="ent">The delivery entity.</param>
    /// <param name="jobProto">A job prototype ID associated with the recipient.</param>
    /// <returns>An entity table selector to use for spawning mail loot.</returns>
    private EntityTableSelector GetDeliveryLootTable(Entity<DeliveryComponent> ent, string jobProto)
    {
        var baseTable = ent.Comp.BaseTable;
        var deptTables = ent.Comp.DepartmentTables;
        var jobTables = ent.Comp.JobTables;

        // nothin else to pick buddy
        if (deptTables.Count == 0 && jobTables.Count == 0
            || !_protoMan.TryIndex<JobPrototype>(jobProto, out var job))
            return baseTable;

        if (jobTables.TryGetValue(job.ID, out var jobTable))
            return jobTable;

        // oh god
        if ((_job.TryGetPrimaryDepartment(job.ID, out var department) || _job.TryGetDepartment(job.ID, out department))
            && deptTables.TryGetValue(department.ID, out var deptTable))
            return deptTable;

        return baseTable;
    }
}
