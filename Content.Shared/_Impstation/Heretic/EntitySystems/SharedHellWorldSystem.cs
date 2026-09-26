using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Shared._Impstation.Heretic.EntitySystems;

public abstract class SharedHellWorldSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public void FlingDroppedEntity(EntityUid target) //this code is direct from GibbingSystem, but inside another system so i can't get to it >:(
    {
        var impulse = 25 + _random.NextFloat(5);
        var scatterVec = _random.NextAngle().ToVec() * impulse;
        _physics.ApplyLinearImpulse(target, scatterVec);
    }
}
