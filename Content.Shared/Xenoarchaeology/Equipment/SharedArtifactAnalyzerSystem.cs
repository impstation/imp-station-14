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

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArtifactAnalyzerComponent, ItemPlacedEvent>(OnItemPlaced);
        SubscribeLocalEvent<ArtifactAnalyzerComponent, ItemRemovedEvent>(OnItemRemoved);
        SubscribeLocalEvent<ArtifactAnalyzerComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<AnalysisConsoleComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<AnalysisConsoleComponent, PortDisconnectedEvent>(OnPortDisconnected);
    }

    private void OnItemPlaced(Entity<ArtifactAnalyzerComponent> ent, ref ItemPlacedEvent args)
    {
        ent.Comp.CurrentArtifact = args.OtherEntity;
        // imp edit start
        // give whatever's on the pad the biased component
        var bias = EnsureComp<XenoArtifactBiasedComponent>(args.OtherEntity);
        if (ent.Comp.Console != null)
            bias.Provider = ent.Comp.Console.Value;

        //If the pad has a linked advanced node scanner, let the artifact know
        if (ent.Comp.AdvancedNodeScanner != null && TryComp<XenoArtifactComponent>(args.OtherEntity, out var artifact))
            artifact.AdvancedNodeScanner = ent.Comp.AdvancedNodeScanner;
        // imp edit end
        Dirty(ent);
    }

    private void OnItemRemoved(Entity<ArtifactAnalyzerComponent> ent, ref ItemRemovedEvent args)
    {
        //imp edit start
        if (TryComp<XenoArtifactComponent>(args.OtherEntity, out var artifact))
            artifact.AdvancedNodeScanner = null;
        //imp edit end

        if (args.OtherEntity != ent.Comp.CurrentArtifact)
            return;

        ent.Comp.CurrentArtifact = null;
        // imp edit start, okay now take it away
        if (TryComp<XenoArtifactBiasedComponent>(args.OtherEntity, out var bias) && ent.Comp.Console != null && bias.Provider == ent.Comp.Console.Value)
            RemComp(args.OtherEntity, bias);
        // imp edit end
        Dirty(ent);
    }

    private void OnMapInit(Entity<ArtifactAnalyzerComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<DeviceLinkSinkComponent>(ent, out var sink))
            return;

        foreach (var source in sink.LinkedSources)
        {
            //#IMP modify this foreach to include advanced node scanner
            if (TryComp<AnalysisConsoleComponent>(source, out var analysis))
            {
                analysis.AnalyzerEntity = GetNetEntity(ent);
                ent.Comp.Console = source;

                if (ent.Comp.AdvancedNodeScanner is { } advancedNodeScanner)
                {
                    analysis.AdvancedNodeScanner = GetNetEntity(advancedNodeScanner);
                }
                Dirty(source, analysis);
                Dirty(ent);
            }

            if (TryComp<AdvancedNodeScannerComponent>(source, out var advanced))
            {
                advanced.AnalyzerEntity = GetNetEntity(ent);
                ent.Comp.AdvancedNodeScanner = source;

                if (ent.Comp.Console is { } console && TryComp<AnalysisConsoleComponent>(console, out var analysisConsole))
                {
                    analysisConsole.AdvancedNodeScanner = GetNetEntity(source);
                }
                Dirty(source, advanced);
                Dirty(ent);
            }
        }
    }

    private void OnNewLink(Entity<AnalysisConsoleComponent> ent, ref NewLinkEvent args)
    {
        if (!TryComp<ArtifactAnalyzerComponent>(args.Sink, out var analyzer))
            return;

        ent.Comp.AnalyzerEntity = GetNetEntity(args.Sink);
        analyzer.Console = ent;

        // #IMP
        if (analyzer.AdvancedNodeScanner is { } advanced)
            ent.Comp.AdvancedNodeScanner = GetNetEntity(advanced); //#IMP

        Dirty(args.Sink, analyzer);
        Dirty(ent);
    }

    private void OnPortDisconnected(Entity<AnalysisConsoleComponent> ent, ref PortDisconnectedEvent args)
    {
        var analyzerNetEntity = ent.Comp.AnalyzerEntity;
        if (args.Port != ent.Comp.LinkingPort || analyzerNetEntity == null)
            return;

        var analyzerEntityUid = GetEntity(analyzerNetEntity);
        if (TryComp<ArtifactAnalyzerComponent>(analyzerEntityUid, out var analyzer))
        {
            analyzer.Console = null;
            Dirty(analyzerEntityUid.Value, analyzer);
        }

        //#IMP
        ent.Comp.AdvancedNodeScanner = null;

        ent.Comp.AnalyzerEntity = null;
        Dirty(ent);
    }

    public bool TryGetAnalyzer(Entity<AnalysisConsoleComponent> ent, [NotNullWhen(true)] out Entity<ArtifactAnalyzerComponent>? analyzer)
    {
        analyzer = null;

        var consoleEnt = ent.Owner;
        if (!_powerReceiver.IsPowered(consoleEnt))
            return false;

        var analyzerUid = GetEntity(ent.Comp.AnalyzerEntity);
        if (!TryComp<ArtifactAnalyzerComponent>(analyzerUid, out var analyzerComp))
            return false;

        if (!_powerReceiver.IsPowered(analyzerUid.Value))
            return false;

        analyzer = (analyzerUid.Value, analyzerComp);
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
        return true;
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
