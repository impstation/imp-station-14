using System.Diagnostics.CodeAnalysis;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Equipment.Components;
using Content.Shared.Xenoarchaeology.Artifact;

namespace Content.Shared.Xenoarchaeology.Equipment;

/// <summary>
/// This system is used for managing the advanced node scanner.
/// It handles linking, helper functions, and announcing scanned nodes.
/// </summary>
public abstract class SharedAdvancedNodeScannerSystem : EntitySystem
{
    [Dependency] private readonly SharedPowerReceiverSystem _powerReceiver = default!;
    [Dependency] private readonly SharedArtifactAnalyzerSystem _analyzer = default!;
    [Dependency] private readonly SharedXenoArtifactSystem _artifact = default!;


    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AdvancedNodeScannerComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<AdvancedNodeScannerComponent, PortDisconnectedEvent>(OnPortDisconnected);

        SubscribeLocalEvent<XenoArtifactComponent, ArtifactUnlockingFinishedEvent>(OnUnlockingFinished);
    }


    private void OnNewLink(Entity<AdvancedNodeScannerComponent> ent, ref NewLinkEvent args)
    {
        if (!TryComp<ArtifactAnalyzerComponent>(args.Sink, out var analyzer))
            return;

        ent.Comp.AnalyzerEntity = args.Sink;
        analyzer.AdvancedNodeScanner = ent;

        if (analyzer.CurrentArtifact is { } artifact && TryComp<XenoArtifactComponent>(artifact, out var artifactComp))
        {
            _artifact.SetAdvancedNodeScanner((artifact, artifactComp), ent.Owner);
            Dirty(artifact, artifactComp);
        }

        Dirty(args.Sink, analyzer);
        Dirty(ent);
    }

    private void OnPortDisconnected(Entity<AdvancedNodeScannerComponent> ent, ref PortDisconnectedEvent args)
    {
        if (args.Port != ent.Comp.AdvancedNodeScannerLinkingPort || ent.Comp.AnalyzerEntity is not { } analyzerEntity)
            return;

        if (TryComp<ArtifactAnalyzerComponent>(analyzerEntity, out var analyzer))
        {
            analyzer.AdvancedNodeScanner = null;
            Dirty(analyzerEntity, analyzer);

            if (analyzer.Console is { } console && TryComp<AnalysisConsoleComponent>(analyzer.Console, out var analysisConsoleComponent))
            {
                analysisConsoleComponent.AdvancedNodeScanner = null;
                Dirty(console, analysisConsoleComponent);
            }

            if (analyzer.CurrentArtifact is { } artifact && TryComp<XenoArtifactComponent>(artifact, out var artifactComp))
            {
                _artifact.SetAdvancedNodeScanner((artifact, artifactComp), null);
                Dirty(artifact, artifactComp);
            }
        }

        ent.Comp.AnalyzerEntity = null;
        Dirty(ent);
    }

    private void OnUnlockingFinished(Entity<XenoArtifactComponent> ent, ref ArtifactUnlockingFinishedEvent args)
    {
        //ANSTODO get unlocking component and pass to helper function
    }

    /// ANSTODO: get data on update ticks if time has passed - trigger helper function
    /// ANSTODO: helper function should take all info from unlocking, compare to previous so new info is available
    ///     then 'advertise'-type say the difference
    ///     and update the historic data

    public bool TryGetAdvancedNodeScanner(Entity<AnalysisConsoleComponent> ent, [NotNullWhen(true)] out Entity<AdvancedNodeScannerComponent>? advancedNodeScanner)
    {
        advancedNodeScanner = null;
        if (!_analyzer.TryGetAnalyzer(ent, out var analyzer))
            return false;

        if (analyzer.Value.Comp.AdvancedNodeScanner is not { } advancedNodeScannerUid)
            return false;

        if (!TryComp<AdvancedNodeScannerComponent>(advancedNodeScannerUid, out var ansComp))
            return false;

        if (!_powerReceiver.IsPowered(advancedNodeScannerUid))
            return false;

        advancedNodeScanner = (advancedNodeScannerUid, ansComp);
        return true;
    }

}
