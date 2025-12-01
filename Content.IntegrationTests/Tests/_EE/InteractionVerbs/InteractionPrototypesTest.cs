using Content.Shared._EE.InteractionVerbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._EE.InteractionVerbs;

[TestFixture]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
[TestOf(typeof(InteractionVerbPrototype))]
public sealed class InteractionPrototypesTest
{
    public const string TestMobProto = "MobHuman";

    [Test]
    public async Task ValidatePrototypeContents()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var server = pair.Server;
        await server.WaitIdleAsync();

        var entMan = server.ResolveDependency<IEntityManager>();
        var protoMan = server.ResolveDependency<IPrototypeManager>();

        // TODO probably should test if an entity receives an abstract verb, but Iunno how
        foreach (var proto in protoMan.EnumeratePrototypes<InteractionVerbPrototype>())
        {
            Assert.That(proto.Abstract || proto.Action is not null, $"Non-abstract prototype {proto.ID} lacks an action!");
        }


        await pair.CleanReturnAsync();
    }
}
