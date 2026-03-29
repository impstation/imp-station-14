using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
    [Dependency] private readonly SharedXenoArtifactSystem _artifact = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AdvancedNodeScannerComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<AdvancedNodeScannerComponent, PortDisconnectedEvent>(OnPortDisconnected);
        SubscribeLocalEvent<AdvancedNodeScannerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<XenoArtifactComponent, ArtifactUnlockingFinishedEvent>(OnUnlockingFinished);
    }

    private void OnMapInit(EntityUid uid, AdvancedNodeScannerComponent comp, MapInitEvent args)
    {
        UpdateANSLinkAppearance((uid, comp), comp.AnalyzerEntity is not null);
    }

    private void OnNewLink(Entity<AdvancedNodeScannerComponent> ent, ref NewLinkEvent args)
    {
        if (!TryComp<ArtifactAnalyzerComponent>(args.Sink, out var analyzer))
            return;

        ent.Comp.AnalyzerEntity = args.Sink;
        analyzer.AdvancedNodeScanner = ent;

        // Turn the 'linked' light on
        UpdateANSLinkAppearance(ent, true);

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
        // Turn the 'linked' light off
        UpdateANSLinkAppearance(ent, false);

        ent.Comp.AnalyzerEntity = null;
        Dirty(ent);
    }

    /// <summary>
    /// Updates the appearance of the advanced node scanner - turns the 'linked' light on or off.
    /// </summary>
    private void UpdateANSLinkAppearance(Entity<AdvancedNodeScannerComponent> ent, bool linked)
    {
        if (!TryComp<AppearanceComponent>(ent.Owner, out var appearance))
            return;

        Appearance.SetData(ent.Owner, AdvancedNodeScannerVisuals.Linked, linked, appearance);
    }

    /// <summary>
    /// Inform every advanced node scanner (including unpowered) to flush the unlocking session from active monitoring
    /// into unlock history memory. ANS will advertise the session finish and result ONLY if  powered and the artifact is
    /// on pad linked it.
    /// </summary>
    /// <param name="ent">The artifact which finished unlocking</param>
    /// <param name="args">Contains the result of the unlock session</param>
    private void OnUnlockingFinished(Entity<XenoArtifactComponent> ent, ref ArtifactUnlockingFinishedEvent args)
    {
        var netArtifact = GetNetEntity(ent.Owner);
        //Advanced node scanners get to magically know if an unlocking session is finished across the map without power whatever
        var query = EntityQueryEnumerator<AdvancedNodeScannerComponent>();
        while (query.MoveNext(out var advancedNodeScannerUid, out var advancedNodeScannerComponent))
        {
            if (advancedNodeScannerComponent.ArtifactUnlockSessions.ContainsKey(netArtifact))
            {
                var session = advancedNodeScannerComponent.ArtifactUnlockSessions[netArtifact];
                session.EndTime = _timing.CurTime;
                if (args.UnlockedNode is { } unlockedNode)
                    session.UnlockedNode = GetNetEntity(unlockedNode).Id;
                else
                    session.UnlockedNode = null;

                if (_powerReceiver.IsPowered((advancedNodeScannerUid)) &&
                    ent.Comp.AdvancedNodeScanner == advancedNodeScannerUid)
                {
                    // Double-check that we've got all the correct triggered nodes
                    if (TryComp<XenoArtifactUnlockingComponent>(ent.Owner, out var unlockComp))
                    {
                        foreach (var nodeIndex in unlockComp.TriggeredNodeIndexes)
                        {
                            if (!session.ActivatedNodes.Exists(x => x.Index == nodeIndex))
                                RegisterTriggeredNode(ent, _artifact.GetNode(ent, nodeIndex), true);
                        }
                    }

                    //ANS TODO: trigger end of unlock advertise
                }

                // Save the unlock session to Advanced Node Scanner's memory and stop thinking this artifact is unlocking
                if (!advancedNodeScannerComponent.UnlockHistories.ContainsKey(netArtifact.Id))
                    advancedNodeScannerComponent.UnlockHistories[netArtifact.Id] = new List<UnlockSession>();
                advancedNodeScannerComponent.UnlockHistories[netArtifact.Id].Add(session);
                advancedNodeScannerComponent.ArtifactUnlockSessions.Remove(netArtifact);
                Dirty(advancedNodeScannerUid, advancedNodeScannerComponent);
            }
        }
    }

    /// <summary>
    /// Powered Advanced Node Scanner will recognize which node(or artifexium) was triggered, list it in its memory about
    /// the artifact unlock session. Will advertise the changes if advertisement is turned on for this advanced node scanner.
    /// </summary>
    /// <param name="artifact">The artifact that has had a node trigger</param>
    /// <param name="node">The node which triggered, null if artifex is applied</param>
    /// <param name="ignoreTime">If we should not list the current time as the time the node was triggered - use if checking for historical triggers</param>
    public void RegisterTriggeredNode(Entity<XenoArtifactComponent> artifact, Entity<XenoArtifactNodeComponent>? node, bool ignoreTime = false)
    {
        if (artifact.Comp.AdvancedNodeScanner is not { } advancedNodeScannerUid || !_powerReceiver.IsPowered(advancedNodeScannerUid))
            return;

        if (!TryComp<AdvancedNodeScannerComponent>(advancedNodeScannerUid, out var advancedNodeScannerComponent))
            return;

        var netArtifact = GetNetEntity(artifact.Owner);

        // Predict what the end time will be if we don't get any more correct triggers
        TimeSpan? predictedEndTime = null;
        if (TryComp<XenoArtifactUnlockingComponent>(artifact.Owner, out var unlock))
            predictedEndTime = unlock.EndTime;

        TimeSpan? now = _timing.CurTime;
        if (!advancedNodeScannerComponent.ArtifactUnlockSessions.ContainsKey(netArtifact))
        {
            var session = new UnlockSession(netArtifact.Id, MetaData(artifact.Owner).EntityName, now.Value, predictedEndTime, [], false, null);
            advancedNodeScannerComponent.ArtifactUnlockSessions.Add(netArtifact, session);
        }

        if (ignoreTime)
            now = null;

        //var toAdvertise = new List<int>(); placeholder for advertising in future

        var sessionUpdate = advancedNodeScannerComponent.ArtifactUnlockSessions[netArtifact];
        if (node == null)
        {
            sessionUpdate.ArtifexiumApplied = true;
            sessionUpdate.ActivatedNodes.Add(new NodeActivation(now, -1, null, null, "artifexium"));
            //toAdvertise.Add(-1);
        }
        else
        {
            var index = _artifact.GetIndex(artifact, node.Value.Owner);
            var triggerTip = node.Value.Comp.TriggerTip is null ? "" : Loc.GetString(node.Value.Comp.TriggerTip);
            sessionUpdate.ActivatedNodes.Add(new NodeActivation(
                now,
                index,
                GetNetEntity(node.Value.Owner).Id,
                _artifact.GetNodeId(node.Value.Owner),
                triggerTip));
            //toAdvertise.Add(index);
        }

        if (predictedEndTime != null)
            sessionUpdate.EndTime = predictedEndTime;

        advancedNodeScannerComponent.ArtifactUnlockSessions[netArtifact] = sessionUpdate;
        Dirty(advancedNodeScannerUid, advancedNodeScannerComponent);
        // ANS TODO: advertise - (-1) index means artifexium
    }

    /// <summary>
    ///  When artifact goes back on pad, check for triggered nodes that were triggered when artifact was away.
    ///  Only checks if advanced node scanner is powered, and only applicable if artifact is in unlock state
    /// </summary>
    /// <param name="advancedNodeScannerUid"></param>
    /// <param name="artifact"></param>
    public void CheckForTriggeredNodes(EntityUid advancedNodeScannerUid, Entity<XenoArtifactComponent> artifact)
    {
        if (!_powerReceiver.IsPowered(advancedNodeScannerUid))
            return;

        if (!TryComp<XenoArtifactUnlockingComponent>(artifact.Owner, out var unlock))
            return;

        if (!TryComp<AdvancedNodeScannerComponent>(advancedNodeScannerUid, out var advancedNodeScannerComponent))
            return;

        if (!advancedNodeScannerComponent.ArtifactUnlockSessions.TryGetValue(GetNetEntity(artifact.Owner), out var session))
        {
            // We don't know about the unlock session at all
            var nodeIndex = unlock.TriggeredNodeIndexes.FirstOrDefault();
            RegisterTriggeredNode(artifact, _artifact.GetNode(artifact, nodeIndex));
            session = advancedNodeScannerComponent.ArtifactUnlockSessions[GetNetEntity(artifact.Owner)];
        }

        foreach (var nodeIndex in unlock.TriggeredNodeIndexes)
        {
            if (!session.ActivatedNodes.Exists(x => x.Index == nodeIndex))
                RegisterTriggeredNode(artifact, _artifact.GetNode(artifact, nodeIndex), true);
        }

    }

    public bool TryGetAdvancedNodeScanner(Entity<AnalysisConsoleComponent> ent, [NotNullWhen(true)] out Entity<AdvancedNodeScannerComponent>? advancedNodeScanner)
    {
        advancedNodeScanner = null;

        var consoleEnt = ent.Owner;
        if (!_powerReceiver.IsPowered(consoleEnt))
            return false;

        if (!TryComp<ArtifactAnalyzerComponent>(ent.Comp.AnalyzerEntity, out var analyzerComp))
            return false;

        if (!_powerReceiver.IsPowered(ent.Comp.AnalyzerEntity.Value))
            return false;

        if (analyzerComp.AdvancedNodeScanner is not { } advancedNodeScannerUid)
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

    /// <summary>
    /// Gets the latest unlock session for a particular artifact, as witnessed by linked advanced node scanner.
    /// </summary>
    /// <param name="ent"> Artifact </param>
    /// <returns> Latest UnlockSession - current or past </returns>
    public UnlockSession? GetLatestUnlockSession(Entity<XenoArtifactComponent> ent)
    {
        if (ent.Comp.AdvancedNodeScanner is null ||
            !TryComp<AdvancedNodeScannerComponent>(ent.Comp.AdvancedNodeScanner, out var ans))
            return null;

        var netArtifact = GetNetEntity(ent.Owner);

        if (ans.ArtifactUnlockSessions.TryGetValue(netArtifact, out var session))
            return session;

        if (!ans.UnlockHistories.TryGetValue(netArtifact.Id, out var history))
            return null;

        return history.Last();
    }

    /// <summary>
    /// Gets the latest unlock session for a particular artifact, as witnessed by linked advanced node scanner.
    /// </summary>
    /// <param name="artifact"> Artifact </param>
    /// <param name="ans"> Advanced node scanner </param>
    /// <returns> Latest UnlockSession - current or past </returns>
    public UnlockSession? GetLatestUnlockSession(Entity<XenoArtifactComponent> artifact, Entity<AdvancedNodeScannerComponent> ans)
    {
        if (artifact.Comp.AdvancedNodeScanner != ans.Owner)
            return null;
        var netArtifact = GetNetEntity(artifact.Owner);

        if (ans.Comp.ArtifactUnlockSessions.TryGetValue(netArtifact, out var session))
            return session;

        if (ans.Comp.UnlockHistories.TryGetValue(netArtifact.Id, out var sessions))
            return sessions.Last();

        return null;
    }

    /// <summary>
    /// Gets the current unlock session for a particular artifact, as witnessed by linked advanced node scanner.
    /// Does not return historic unlock sessions.
    /// </summary>
    /// <param name="artifact"> Artifact </param>
    /// <param name="ans"> Artifact </param>
    /// <returns> Latest UnlockSession - current or past </returns>
    public UnlockSession? GetCurrentUnlockSession(Entity<XenoArtifactComponent> artifact, Entity<AdvancedNodeScannerComponent> ans)
    {
        if (artifact.Comp.AdvancedNodeScanner != ans.Owner)
            return null;

        if (ans.Comp.ArtifactUnlockSessions.TryGetValue(GetNetEntity(artifact.Owner), out var session))
            return session;

        return null;
    }

}
