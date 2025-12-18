using Content.Shared._ES.TileFires;
using Content.Shared.Physics;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._ES.Sparks;

public sealed class ESSparksSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ESSharedTileFireSystem _tileFire = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public static readonly EntProtoId DefaultSparks = "ESEffectSparks";

    [PublicAPI]
    public void DoSparks(EntityUid source, int number = 4, EntProtoId? sparksPrototype = null, float tileFireChance = 0f)
    {
        var coords = Transform(source).Coordinates;
        DoSparks(coords, number, sparksPrototype, source, tileFireChance);
    }

    [PublicAPI]
    public void DoSparks(EntityCoordinates coordinates, int number = 4, EntProtoId? sparksPrototype = null, EntityUid? ignored = null, float tileFireChance = 0f)
    {
        if (_net.IsClient)
            return;

        sparksPrototype ??= DefaultSparks;

        var angleDelta = (Angle) (MathF.Tau / number);
        var angle = _random.NextAngle();
        for (var i = 0; i < number; i++)
        {
            var sparks = Spawn(sparksPrototype, _transform.ToMapCoordinates(coordinates), rotation: angle);
            angle += angleDelta;
            _throwing.TryThrow(sparks, angle.ToVec(), 2f, animated: false);
            PreventCollide(sparks, ignored);
        }

        // TODO sparks should take in user and pass it in here also (arsonist core)
        if (_random.Prob(tileFireChance))
            _tileFire.TryDoTileFire(coordinates, null, _random.Next(1, 4));
    }

    private void PreventCollide(EntityUid sparks, EntityUid? ignored)
    {
        if (!ignored.HasValue || TerminatingOrDeleted(ignored) || EntityManager.IsQueuedForDeletion(ignored.Value))
            return;
        var comp = EnsureComp<PreventCollideComponent>(sparks);
        comp.Uid = ignored.Value;
        Dirty(sparks, comp);
    }
}
