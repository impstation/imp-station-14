using System.Linq;
using System.Numerics;
using Content.Shared.EntityTable;
using Content.Shared.EntityTable.EntitySelectors;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Map;

namespace Content.Shared.Containers;

public sealed class ContainerFillSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ContainerFillComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<EntityTableContainerFillComponent, MapInitEvent>(OnTableMapInit);
    }

    private void OnMapInit(EntityUid uid, ContainerFillComponent component, MapInitEvent args)
    {
        if (!TryComp(uid, out ContainerManagerComponent? containerComp))
            return;

        var xform = Transform(uid);
        var coords = new EntityCoordinates(uid, Vector2.Zero);

        foreach (var (contaienrId, prototypes) in component.Containers)
        {
            if (!_containerSystem.TryGetContainer(uid, contaienrId, out var container, containerComp))
            {
                Log.Error($"Entity {ToPrettyString(uid)} with a {nameof(ContainerFillComponent)} is missing a container ({contaienrId}).");
                continue;
            }

            foreach (var proto in prototypes)
            {
                var ent = Spawn(proto, coords);
                if (!_containerSystem.Insert(ent, container, containerXform: xform))
                {
                    var alreadyContained = container.ContainedEntities.Count > 0 ? string.Join("\n", container.ContainedEntities.Select(e => $"\t - {ToPrettyString(e)}")) : "< empty >";
                    Log.Error($"Entity {ToPrettyString(uid)} with a {nameof(ContainerFillComponent)} failed to insert an entity: {ToPrettyString(ent)}.\nCurrent contents:\n{alreadyContained}");
                    _transform.AttachToGridOrMap(ent);
                    break;
                }
            }
        }
    }

    private void OnTableMapInit(Entity<EntityTableContainerFillComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp(ent, out ContainerManagerComponent? containerComp))
            return;

        if (TerminatingOrDeleted(ent) || !Exists(ent))
            return;

        var xform = Transform(ent);
        var coords = new EntityCoordinates(ent, Vector2.Zero);

        foreach (var (containerId, table) in ent.Comp.Containers)
        {
            // IMP edit: what if i just moved it out. what are you gonna do about that huh.
            FillContainer((ent.Owner, containerComp),
                containerId,
                table,
                coords,
                xform,
                nameof(EntityTableContainerFillComponent));
        }
    }

    // IMP start: just move this shit into a public API who cares

    /// <summary>
    /// Resolves an entity table into spawned entities, and then fills an entity's container with the spawned entities.
    /// </summary>
    /// <remarks>
    /// This is used by EntityTableContainerFillComponent to fill containers upon initialization.
    /// </remarks>
    /// <param name="ent">The entity with containers to fill.</param>
    /// <param name="containerId">The ID of the container to fill.</param>
    /// <param name="table">The entity table to populate the container with.</param>
    /// <param name="coords">Spawn coordinates for the table entities.</param>
    /// <param name="xform">The transform component of the container entity.</param>
    /// <param name="componentName">The name of the component invoking this, for logging purposes.</param>
    [PublicAPI]
    public void FillContainer(Entity<ContainerManagerComponent?> ent,
        string containerId,
        EntityTableSelector table,
        EntityCoordinates? coords = null,
        TransformComponent? xform = null,
        string? componentName = null)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, logMissing: false))
            return;

        xform ??= Transform(ent);
        coords ??= new EntityCoordinates(ent, Vector2.Zero);
        var componentText = componentName == null ? "" : $" with a {componentName}";

        if (!_containerSystem.TryGetContainer(ent.Owner, containerId, out var container, ent.Comp))
        {
            Log.Error($"Entity {ToPrettyString(ent)}{componentText} is missing a container ({containerId}).");
            return;
        }

        var spawns = _entityTable.GetSpawns(table);
        foreach (var proto in spawns)
        {
            var spawn = Spawn(proto, coords.Value);
            if (!_containerSystem.Insert(spawn, container, containerXform: xform))
            {
                var alreadyContained = container.ContainedEntities.Count > 0 ? string.Join("\n", container.ContainedEntities.Select(e => $"\t - {ToPrettyString(e)}")) : "< empty >";
                Log.Error($"Entity {ToPrettyString(ent)}{componentText} failed to insert an entity: {ToPrettyString(spawn)}.\nCurrent contents:\n{alreadyContained}");
                _transform.AttachToGridOrMap(spawn);
                break;
            }
        }
    }
    // IMP end
}
