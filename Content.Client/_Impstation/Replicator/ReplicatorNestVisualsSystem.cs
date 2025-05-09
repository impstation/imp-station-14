using Content.Shared._Impstation.Replicator;
using Robust.Client.GameObjects;

namespace Content.Client._Impstation.Replicator;

public sealed partial class ReplicatorNestVisualsSystem : SharedReplicatorNestSystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReplicatorNestComponent, ReplicatorNestEmbiggenedEvent>(OnEmbiggened);
    }

    private void OnEmbiggened(Entity<ReplicatorNestComponent> ent, ref ReplicatorNestEmbiggenedEvent args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        var targetLayer = ReplicatorNestVisuals.Level1; // this would probably be more readable as a switch but i dont want to use a switch for three cases
        if (ent.Comp.CurrentLevel >= 3)
        {
            targetLayer = ReplicatorNestVisuals.Level3;
        }
        else if (ent.Comp.CurrentLevel == 2)
        {
            targetLayer = ReplicatorNestVisuals.Level2;
        }

        if (!sprite.LayerMapTryGet(targetLayer, out var layerIndex))
            return;

        sprite.LayerSetVisible(layerIndex, true);

        _appearance.OnChangeData(ent.Owner, sprite);
    }
}