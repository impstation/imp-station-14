using System.Linq;
using Content.Server.GameTicking.Rules;
using Robust.Shared.Random;

namespace Content.Server._Impstation.GameTicking.Rules.VariationPass;

/// <summary>
///     Base class for procedural player variation rule passes, which apply some kind of variation to players,
///     so we simply reduce the boilerplate for the event handling a bit with this.
/// </summary>
public abstract class PlayerVariationPassSystem<T> : GameRuleSystem<T>
    where T : IComponent
{
    [Dependency] protected readonly IRobustRandom Random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<T, PlayerVariationPassEvent>(ApplyPlayerVariation);
    }

    protected List<EntityUid> GetPlayers(int retrieve, bool removeFromList, ref PlayerVariationPassEvent args)
    {
        var pool = removeFromList ? args.Players : args.Players.ToList();
        var playerEnts = new List<EntityUid>();
        var count = Math.Min(retrieve, pool.Count);

        for (var i = 0; i < count; i++)
        {
            var player = Random.PickAndTake(pool);
            playerEnts.Add(player);
        }

        return playerEnts;
    }

    protected abstract void ApplyPlayerVariation(Entity<T> ent, ref PlayerVariationPassEvent args);
}
