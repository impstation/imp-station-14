using System.Collections.Generic;
using System.Linq;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._DEN.Solutions;

[TestFixture]
[TestOf(typeof(SolutionContainerManagerComponent))]
public sealed class ValidSolutionTest
{
    /// <summary>
    ///     Checks all entity prototypes in the game to make sure that the contents of their solution
    ///     do not exceed the maximum volume the solution contains. Otherwise, if the entity is spawned
    ///     in-game, it will spill its excess contents on the floor.
    /// </summary>
    [Test]
    public async Task AllSolutionsWithinCapacity()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var client = pair.Client;
        var componentFactory = server.ResolveDependency<IComponentFactory>();
        var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();
        var failedPrototypes = new Dictionary<string, string>();

        var testMap = await pair.CreateTestMap(false);

        await server.WaitAssertion(() =>
        {
            var grid = server.MapMan.CreateGridEntity(testMap.MapId);
            var coord = new EntityCoordinates(grid.Owner, 0, 0);

            foreach (var prototype in server.ProtoMan.EnumeratePrototypes<EntityPrototype>()
                .Where(p => !p.Abstract
                    && !pair.IsTestPrototype(p)
                    && p.HasComponent<SolutionContainerManagerComponent>(componentFactory)))
            {
                var ent = server.EntMan.SpawnEntity(prototype.ID, coord);

                if (!server.EntMan.TryGetComponent<SolutionContainerManagerComponent>(ent, out var solutionManager)
                    || solutionManager.Solutions is null)
                    continue;

                var failedSolutions = new List<string>();
                foreach (var kvp in solutionManager.Solutions)
                {
                    var solution = kvp.Value;
                    if (solution.Volume > solution.MaxVolume)
                        failedSolutions.Add($"{kvp.Key}: {solution.Volume}/{solution.MaxVolume}");
                }

                if (failedSolutions.Count > 0)
                    failedPrototypes.Add(prototype.ID, string.Join(", ", failedSolutions));
            }

            Assert.That(failedPrototypes, Is.Empty,
                $"The following entities have solutions with greater volume than their capacity:\n  "
                + string.Join("\n  ", failedPrototypes.Select(kvp => $"{kvp.Key}: {kvp.Value}")));
        });

        await pair.CleanReturnAsync();
    }
}
