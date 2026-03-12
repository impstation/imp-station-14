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

    private void PopulateContents(Entity<DeliveryComponent> ent, string jobProto)
    {
        var containerId = ent.Comp.Container;
        var table = GetDeliveryLootTable(ent, jobProto);

        _containerFill.FillContainer(ent.Owner, containerId, table);
    }

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
