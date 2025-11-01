using System.Diagnostics.CodeAnalysis;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Equipment.Components;
using Content.Shared.Xenoarchaeology.Artifact;

namespace Content.Shared.Xenoarchaeology.Equipment;

/// <summary>
/// This system is used for managing the artifact analyzer as well as the analysis console.
/// It also handles scanning and ui updates for both systems.
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
    }


    private void OnNewLink(Entity<AdvancedNodeScannerComponent> ent, ref NewLinkEvent args)
    {
        if (!TryComp<ArtifactAnalyzerComponent>(args.Sink, out var analyzer))
            return;

        ent.Comp.AnalyzerEntity = GetNetEntity(args.Sink);
        analyzer.AdvancedNodeScanner = ent;
        if (analyzer.Console is not null && TryComp<AnalysisConsoleComponent>(analyzer.Console, out var analysisConsoleComponent))
        {
            analysisConsoleComponent.AdvancedNodeScanner = GetNetEntity(ent);
            Dirty((EntityUid)analyzer.Console, analysisConsoleComponent);
        }
        if (analyzer.CurrentArtifact is not null && TryComp<XenoArtifactComponent>(analyzer.CurrentArtifact, out var artifactComp))
        {
            _artifact.SetAdvancedNodeScanner(((EntityUid)analyzer.CurrentArtifact, artifactComp), ent.Owner);
            Dirty((EntityUid)analyzer.CurrentArtifact, artifactComp);
        }

        Dirty(args.Sink, analyzer);
        Dirty(ent);
    }

    private void OnPortDisconnected(Entity<AdvancedNodeScannerComponent> ent, ref PortDisconnectedEvent args)
    {
        var analyzerNetEntity = ent.Comp.AnalyzerEntity;
        if (args.Port != ent.Comp.AdvancedNodeScannerLinkingPort || analyzerNetEntity == null)
            return;

        var analyzerEntityUid = GetEntity(analyzerNetEntity);
        if (TryComp<ArtifactAnalyzerComponent>(analyzerEntityUid, out var analyzer))
        {
            analyzer.AdvancedNodeScanner = null;
            Dirty(analyzerEntityUid.Value, analyzer);

            if (analyzer.Console is not null && TryComp<AnalysisConsoleComponent>(analyzer.Console, out var analysisConsoleComponent))
            {
                analysisConsoleComponent.AdvancedNodeScanner = null;
                Dirty((EntityUid)analyzer.Console, analysisConsoleComponent);
            }

            if (analyzer.CurrentArtifact is not null && TryComp<XenoArtifactComponent>(analyzer.CurrentArtifact, out var artifactComp))
            {
                _artifact.SetAdvancedNodeScanner(((EntityUid)analyzer.CurrentArtifact, artifactComp), null);
                Dirty((EntityUid)analyzer.CurrentArtifact, artifactComp);
            }
        }

        ent.Comp.AnalyzerEntity = null;
        Dirty(ent);
    }

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
