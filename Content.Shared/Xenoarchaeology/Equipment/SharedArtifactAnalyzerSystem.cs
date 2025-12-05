using System.Diagnostics.CodeAnalysis;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Placeable;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Equipment.Components;
using Content.Shared._Impstation.Xenoarchaeology.Artifact.Components; // imp edit

namespace Content.Shared.Xenoarchaeology.Equipment;

/// <summary>
/// This system is used for managing the artifact analyzer as well as the analysis console.
/// It also handles scanning and ui updates for both systems.
/// </summary>
public abstract class SharedArtifactAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly SharedPowerReceiverSystem _powerReceiver = default!;
    [Dependency] private readonly SharedDeviceLinkSystem _deviceLink = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArtifactAnalyzerComponent, ItemPlacedEvent>(OnItemPlaced);
        SubscribeLocalEvent<ArtifactAnalyzerComponent, ItemRemovedEvent>(OnItemRemoved);
        SubscribeLocalEvent<ArtifactAnalyzerComponent, NewLinkEvent>(OnNewLinkAnalyzer);
        SubscribeLocalEvent<ArtifactAnalyzerComponent, LinkAttemptEvent>(OnLinkAttemptAnalyzer);
        SubscribeLocalEvent<ArtifactAnalyzerComponent, PortDisconnectedEvent>(OnPortDisconnectedAnalyzer);

        SubscribeLocalEvent<AnalysisConsoleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AnalysisConsoleComponent, NewLinkEvent>(OnNewLinkConsole);
        SubscribeLocalEvent<AnalysisConsoleComponent, LinkAttemptEvent>(OnLinkAttemptConsole);
        SubscribeLocalEvent<AnalysisConsoleComponent, PortDisconnectedEvent>(OnPortDisconnectedConsole);
    }

    private void OnItemPlaced(Entity<ArtifactAnalyzerComponent> ent, ref ItemPlacedEvent args)
    {
        ent.Comp.CurrentArtifact = args.OtherEntity;
        // imp edit start, give whatever's on the pad the biased component
        var bias = EnsureComp<XenoArtifactBiasedComponent>(args.OtherEntity);
        if (ent.Comp.Console != null)
            bias.Provider = ent.Comp.Console.Value;
        // imp edit end
        Dirty(ent);
    }

    private void OnItemRemoved(Entity<ArtifactAnalyzerComponent> ent, ref ItemRemovedEvent args)
    {
        if (args.OtherEntity != ent.Comp.CurrentArtifact)
            return;

        ent.Comp.CurrentArtifact = null;
        // imp edit start, okay now take it away
        if (TryComp<XenoArtifactBiasedComponent>(args.OtherEntity, out var bias) && ent.Comp.Console != null && bias.Provider == ent.Comp.Console.Value)
            RemComp(args.OtherEntity, bias);
        // imp edit end
        Dirty(ent);
    }

    private void OnMapInit(Entity<AnalysisConsoleComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<DeviceLinkSourceComponent>(ent, out var source))
            return;

        var linkedEntities = _deviceLink.GetLinkedSinks((ent.Owner, source), ent.Comp.LinkingPort);

        foreach (var sink in linkedEntities)
        {
            if (!TryComp<ArtifactAnalyzerComponent>(sink, out var analyzer))
                continue;

            ent.Comp.AnalyzerEntity = sink;
            analyzer.Console = ent.Owner;
            Dirty(ent);
            Dirty(sink, analyzer);
            break;
        }
    }

    private void OnNewLinkConsole(Entity<AnalysisConsoleComponent> ent, ref NewLinkEvent args)
    {
        if (args.SourcePort != ent.Comp.LinkingPort || !HasComp<ArtifactAnalyzerComponent>(args.Sink))
            return;

        ent.Comp.AnalyzerEntity = args.Sink;
        Dirty(ent);
    }

    private void OnNewLinkAnalyzer(Entity<ArtifactAnalyzerComponent> ent, ref NewLinkEvent args)
    {
        if (args.SinkPort != ent.Comp.LinkingPort || !HasComp<AnalysisConsoleComponent>(args.Source))
            return;

        ent.Comp.Console = args.Source;
        Dirty(ent);
    }

    private void OnLinkAttemptConsole(Entity<AnalysisConsoleComponent> ent, ref LinkAttemptEvent args)
    {
        if (ent.Comp.AnalyzerEntity != null)
            args.Cancel(); // can only link to one device at a time
    }

    private void OnLinkAttemptAnalyzer(Entity<ArtifactAnalyzerComponent> ent, ref LinkAttemptEvent args)
    {
        if (ent.Comp.Console != null)
            args.Cancel(); // can only link to one device at a time
    }

    private void OnPortDisconnectedConsole(Entity<AnalysisConsoleComponent> ent, ref PortDisconnectedEvent args)
    {
        if (args.Port != ent.Comp.LinkingPort || ent.Comp.AnalyzerEntity == null)
            return;

        ent.Comp.AnalyzerEntity = null;
        Dirty(ent);
    }

    private void OnPortDisconnectedAnalyzer(Entity<ArtifactAnalyzerComponent> ent, ref PortDisconnectedEvent args)
    {
        if (args.Port != ent.Comp.LinkingPort || ent.Comp.Console == null)
            return;

        ent.Comp.Console = null;
        Dirty(ent);
    }

    public bool TryGetAnalyzer(Entity<AnalysisConsoleComponent> ent, [NotNullWhen(true)] out Entity<ArtifactAnalyzerComponent>? analyzer)
    {
        analyzer = null;

        var consoleEnt = ent.Owner;
        if (!_powerReceiver.IsPowered(consoleEnt))
            return false;

        if (!TryComp<ArtifactAnalyzerComponent>(ent.Comp.AnalyzerEntity, out var analyzerComp))
            return false;

        if (!_powerReceiver.IsPowered(ent.Comp.AnalyzerEntity.Value))
            return false;

        analyzer = (ent.Comp.AnalyzerEntity.Value, analyzerComp);
        return true;
    }

    public bool TryGetArtifactFromConsole(Entity<AnalysisConsoleComponent> ent, [NotNullWhen(true)] out Entity<XenoArtifactComponent>? artifact)
    {
        artifact = null;

        if (!TryGetAnalyzer(ent, out var analyzer))
            return false;

        if (!TryComp<XenoArtifactComponent>(analyzer.Value.Comp.CurrentArtifact, out var comp))
            return false;

        artifact = (analyzer.Value.Comp.CurrentArtifact.Value, comp);

        //#IMP doublecheck that artifact is actually ON the pad
        if (Transform(artifact.Value.Owner).Coordinates.TryDistance(EntityManager, Transform(analyzer.Value.Owner).Coordinates, out var distance) && distance < 3)
            return true; // IMP This line wasn't in the if originally
        artifact = null;//IMP
        return false;//IMP
    }

    public bool TryGetAnalysisConsole(Entity<ArtifactAnalyzerComponent> ent, [NotNullWhen(true)] out Entity<AnalysisConsoleComponent>? analysisConsole)
    {
        analysisConsole = null;

        if (!TryComp<AnalysisConsoleComponent>(ent.Comp.Console, out var consoleComp))
            return false;

        analysisConsole = (ent.Comp.Console.Value, consoleComp);
        return true;
    }
}
