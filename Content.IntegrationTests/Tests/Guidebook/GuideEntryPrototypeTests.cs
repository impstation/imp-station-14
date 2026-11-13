using Content.Client.Guidebook;
using Content.Client.Guidebook.Richtext;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using Content.IntegrationTests.Utility;
using Content.Shared.Guidebook;
using Robust.Shared.Localization;

namespace Content.IntegrationTests.Tests.Guidebook;

[TestFixture]
[TestOf(typeof(GuidebookSystem))]
[TestOf(typeof(GuideEntryPrototype))]
[TestOf(typeof(DocumentParsingManager))]
public sealed class GuideEntryPrototypeTests
{
    private static string[] _guideEntries = GameDataScrounger.PrototypesOfKind<GuideEntryPrototype>();

    [Test]
    [TestCaseSource(nameof(_guideEntries))]
    [Description("Ensures a given guidebook entry is valid, checking the document/etc.")]
    //maybe ok?[Ignore("Styleguide update limit")] // imp. i hate this test. this value is arbitrary, it is hardcoded, and it is engineside. the update limit puts a hard cap on how many reagents you can have. and its a WARNING. why does a WARNING fail the test?????? jesus christ. we need dynamic guidebook scrolling
    public async Task Validate(string protoKey)
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var client = pair.Client;
        await client.WaitIdleAsync();
        var protoMan = client.ResolveDependency<IPrototypeManager>();
        var resMan = client.ResolveDependency<IResourceManager>();
        var parser = client.ResolveDependency<DocumentParsingManager>();
        var proto = protoMan.Index<GuideEntryPrototype>(protoKey);

        await client.WaitAssertion(() =>
        {
            using var reader = resMan.ContentFileReadText(proto.Text);
            var text = reader.ReadToEnd();

            Assert.That(parser.TryAddMarkup(new Document(), text), $"Failed to parse the guide entry's document.");
        });

        await pair.CleanReturnAsync();
    }
}
