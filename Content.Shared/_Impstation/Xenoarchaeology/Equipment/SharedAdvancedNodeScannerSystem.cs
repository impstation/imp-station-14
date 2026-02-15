using System.Diagnostics.CodeAnalysis;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Equipment.Components;
using Content.Shared.Xenoarchaeology.Artifact;
using Robust.Shared.Timing;

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
    [Dependency] private readonly IGameTiming _timing = default!;

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
        //Advanced node scanners get to magically know if an unlocking session is finished across the map without power whatever
        var query = EntityQueryEnumerator<AdvancedNodeScannerComponent>();
        while (query.MoveNext(out var advancedNodeScannerUid, out var advancedNodeScannerComponent))
        {
            if (advancedNodeScannerComponent.ArtifactUnlockSessions.ContainsKey(ent.Owner))
            {
                var session = advancedNodeScannerComponent.ArtifactUnlockSessions[ent.Owner];
                session.EndTime = _timing.CurTime;
                session.UnlockedNode = args.UnlockedNode;
                advancedNodeScannerComponent.UnlockHistories[ent.Owner].Add(session);
                //ANS TODO: trigger advertise if powered
                //if (_powerReceiver.IsPowered((advancedNodeScannerUid)))
                //    advertiseChange(session);
                advancedNodeScannerComponent.ArtifactUnlockSessions.Remove(ent.Owner);
            }
        }
    }

    public void RegisterTriggeredNode(Entity<XenoArtifactComponent> artifact, Entity<XenoArtifactNodeComponent>? node, bool force)
    {
        if (artifact.Comp.AdvancedNodeScanner is not { } advancedNodeScannerUid || !_powerReceiver.IsPowered(advancedNodeScannerUid))
            return;

        if (!TryComp<AdvancedNodeScannerComponent>(advancedNodeScannerUid, out var advancedNodeScannerComponent))
            return;

        if (!advancedNodeScannerComponent.ArtifactUnlockSessions.ContainsKey(artifact.Owner))
        {
            var session = new UnlockSession(_timing.CurTime, null, new HashSet<int>(), false, null);
            advancedNodeScannerComponent.ArtifactUnlockSessions.Add(artifact.Owner, session);
        }

        var sessionUpdate = advancedNodeScannerComponent.ArtifactUnlockSessions[artifact.Owner];
        if (node == null)
            sessionUpdate.ArtifexiumApplied = true;
        else
            sessionUpdate.TriggeredNodeIndexes.Add(_artifact.GetIndex(artifact, node.Value.Owner));

        advancedNodeScannerComponent.ArtifactUnlockSessions[artifact.Owner] = sessionUpdate;
        // ANS TODO: advertise
    }

    /// ANS TODO: "advertise" function to say what has changed (togglable with button)


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

    public bool TryGetArtifactFromAdvancedNodeScanner(Entity<AdvancedNodeScannerComponent> ent, [NotNullWhen(true)] out Entity<XenoArtifactComponent>? artifact, bool requirePower = true)
    {
        artifact = null;
        if (!_powerReceiver.IsPowered(ent.Owner) && requirePower)
            return false;

        if (ent.Comp.AnalyzerEntity is not { } analyzerUid)
            return false;

        if (!TryComp<ArtifactAnalyzerComponent>(analyzerUid, out var analyzerComp))
            return false;

        if (analyzerComp.CurrentArtifact is not { } artifactUid)
            return false;

        if (!TryComp<XenoArtifactComponent>(artifactUid, out var artifactComp))
            return false;

        artifact = (artifactUid, artifactComp);
        return true;
    }

}
