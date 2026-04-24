using Content.Shared.Medical;
using Content.shared._Impstation.Medical;

namespace Content.Server._Impstation.Destructible.Thresholds.Behaviors;

[DataDefinition]
public sealed partial class SneezeBehavior : IThresholdBehavior
{
    public void Execute(EntityUid uid, DestructibleSystem system, EntityUid? cause = null)
    {
        system.EntityManager.System<SneezeSystem>().Sneeze(uid);
    }
}
