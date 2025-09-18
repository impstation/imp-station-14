using System.Numerics;
using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.Storage;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

public sealed class SpawnArtifactSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ArtifactSystem _artifact = default!;

    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public const string NodeDataSpawnAmount = "nodeDataSpawnAmount";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpawnArtifactComponent, ArtifactActivatedEvent>(OnActivate);
    }

    private void OnActivate(Entity<SpawnArtifactComponent> ent, ref ArtifactActivatedEvent args)
    {
        if (!_artifact.TryGetNodeData(ent.Owner, NodeDataSpawnAmount, out int amount))
            amount = 0;

        if (amount >= ent.Comp.MaxSpawns)
            return;

        if (ent.Comp.Spawns is not {} spawns)
            return;

        var artifactCord = _transform.GetMapCoordinates(ent);
        foreach (var spawn in EntitySpawnCollection.GetSpawns(spawns, _random))
        {
            var dx = _random.NextFloat(-ent.Comp.Range, ent.Comp.Range);
            var dy = _random.NextFloat(-ent.Comp.Range, ent.Comp.Range);
            var spawnCord = artifactCord.Offset(new Vector2(dx, dy));
            var spawnTarget = Spawn(spawn, spawnCord);
            _transform.AttachToGridOrMap(spawnTarget);

            //#IMP random chance to make ghost role
            if (_random.NextFloat() < ent.Comp.GhostRoleProb && !HasComp<GhostRoleComponent>(spawnTarget))
            {
                if (!TryComp<MetaDataComponent>(spawnTarget, out var meta))
                    continue;

                // Markers should not be ghost roles
                if (meta.EntityPrototype is {} proto && proto.Parents is {} parents && parents.Contains("MarkerBase"))
                    continue;

                var grComp = EnsureComp<GhostRoleComponent>(spawnTarget);
                grComp.RoleName = meta.EntityName;
                grComp.RoleDescription = meta.EntityDescription;
                EnsureComp<GhostTakeoverAvailableComponent>(spawnTarget);
            }
        }
        _artifact.SetNodeData(ent, NodeDataSpawnAmount, amount + 1);
    }
}
