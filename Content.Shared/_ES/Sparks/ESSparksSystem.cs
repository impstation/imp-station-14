using Content.Shared.Physics;
using Content.Shared.Throwing;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._ES.Sparks;

public sealed class ESSparksSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public static readonly EntProtoId DefaultSparks = "ESEffectSparks";

    public void DoSparks(EntityUid source, int number = 4, EntProtoId? sparksPrototype = null)
    {
        var coords = _transform.GetMapCoordinates(source);
        DoSparks(coords, number, sparksPrototype, source);
    }

    public void DoSparks(EntityCoordinates coordinates, int number = 4, EntProtoId? sparksPrototype = null, EntityUid? ignored = null)
    {
        var mapCoordinates = _transform.ToMapCoordinates(coordinates);
        DoSparks(mapCoordinates, number, sparksPrototype, ignored);
    }

    public void DoSparks(MapCoordinates coordinates, int number = 4, EntProtoId? sparksPrototype = null, EntityUid? ignored = null)
    {
        if (_net.IsClient)
            return;

        sparksPrototype ??= DefaultSparks;

        var angleDelta = (Angle) (MathF.Tau / number);
        var angle = _random.NextAngle();
        for (var i = 0; i < number; i++)
        {
            var sparks = EntityManager.Spawn(sparksPrototype, coordinates, rotation: angle);
            angle += angleDelta;
            _throwing.TryThrow(sparks, angle.ToVec(), 2f, animated: false);
            PreventCollide(sparks, ignored);
        }
    }

    private void PreventCollide(EntityUid sparks, EntityUid? ignored)
    {
        if (!ignored.HasValue || TerminatingOrDeleted(ignored))
            return;
        var comp = EnsureComp<PreventCollideComponent>(sparks);
        comp.Uid = ignored.Value;
        Dirty(sparks, comp);
    }
}
