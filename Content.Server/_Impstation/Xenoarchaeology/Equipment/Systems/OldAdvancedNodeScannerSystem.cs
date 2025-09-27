using Content.Server.Xenoarchaeology.XenoArtifacts;
using Content.Server.Power.Components;
using Content.Shared.Construction.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Xenoarchaeology.Equipment.Components;
using Content.Server.Xenoarchaeology.Equipment.Components;
using Robust.Shared.Timing;

using System.Linq;

namespace Content.Server.Xenoarchaeology.Equipment.Systems;

public sealed class OldAdvancedNodeScannerSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly OldArtifactAnalyzerSystem _analyzer = default!;
    [Dependency] private readonly ArtifactSystem _artifact = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ArtifactComponent, ArtifactNodeEnteredEvent>(OnNodeEntered);
    }

    /// <summary>
    /// Is the advanced node scanner capable of scanning (anchored, powered, within range)
    /// </summary>
    /// <param name="advancedNodeScanner">The advanced node scanner</param>
    public bool CanProvideAdvancedScanning(Entity<OldAdvancedNodeScannerComponent> advancedNodeScanner)
    {
        // Must have a linked analyzer
        if (advancedNodeScanner.Comp.AnalyzerEntity is null)
            return false;

        // A.N.S and analyzer must both have transform components.
        if (!TryComp<TransformComponent>(advancedNodeScanner.Owner, out var advancedTransform)
        || !TryComp<TransformComponent>(advancedNodeScanner.Comp.AnalyzerEntity, out var analyzerTransform))
            return false;

        // A.N.S and analyzer must exist on the same map and other distance requirements.
        if (!advancedTransform.Coordinates.TryDistance(EntityManager, analyzerTransform.Coordinates, out var distance))
            return false;

        // If max distance is non-negative, then the distance between analyzer and A.N.S must be less than the max distance.
        if (advancedNodeScanner.Comp.MaxDistanceFromAnalyzerPad > 0 && advancedNodeScanner.Comp.MaxDistanceFromAnalyzerPad < distance)
            return false;

        // If A.N.S has anchorable component, then it must be anchored. (if no anchorable comp then its mapped or admeme and don't care)
        if (HasComp<AnchorableComponent>(advancedNodeScanner.Owner) && !advancedTransform.Anchored)
            return false;

        // If A.N.S has power, then it must be powered (if no power comp then who cares)
        if (TryComp<ApcPowerReceiverComponent>(advancedNodeScanner.Owner, out var power) && !power.Powered)
            return false;

        return true;
    }

    /// <summary>
    /// Catch every instance of the artifact activating and check if it is on a pad linked to advanced node scanner
    /// </summary>
    private void OnNodeEntered(EntityUid uid, ArtifactComponent component, ArtifactNodeEnteredEvent args)
    {
        // Get all pads within 2 tiles, check if they have the artifact on them
        if (!TryComp<TransformComponent>(uid, out var transform))
            return;

        var pads = _lookup.GetEntitiesInRange<OldArtifactAnalyzerComponent>(transform.Coordinates, 2);
        foreach (var pad in pads)
        {
            if (_analyzer.GetArtifactForAnalysis(pad) == uid)
            {
                TryAdvancedScanNodeId(pad);
                return;
            }
        }
    }

    /// <summary>
    /// Get and synchronize artifact ID if advanced node scanner is connected to an analyzer
    /// Also check if trigger/effect changed, (admin intervention) and blank those out
    /// </summary>
    public void TryAdvancedScanNodeId(EntityUid analyzer)
    {
        // Analyzer must have analyzer component
        if (!TryComp<OldArtifactAnalyzerComponent>(analyzer, out var analyzerComponent))
            return;

        // Can't advanced scan without advanced scanner
        var advancedNodeScanner = analyzerComponent.AdvancedNodeScanner;
        if (advancedNodeScanner is null)
            return;

        // advanced scanner needs advanced scan component
        if (!TryComp<OldAdvancedNodeScannerComponent>(advancedNodeScanner, out var ansComp))
            return; // if this happens something has gone wrong or admin intervention has occurred

        // needs to be plugged in and close to pad and such
        var ansEntity = new Entity<OldAdvancedNodeScannerComponent>((EntityUid)advancedNodeScanner, ansComp);
        if (!CanProvideAdvancedScanning(ansEntity))
            return;

        // need an artifact to advanced scan
        var maybeArtifact = _analyzer.GetArtifactForAnalysis(analyzer);
        if (maybeArtifact is null)
            return;

        var artifact = (EntityUid)maybeArtifact;

        // artifact has to have artifact component
        if (!TryComp<ArtifactComponent>(artifact, out var artiComp))
            return; // if this happens then something has gone very wrong

        var currentNodeId = artiComp.CurrentNodeId;

        if (currentNodeId is null)
            return;

        var now = _timing.CurTime;

        //update existing artifact data
        if (ansComp.ScannedArtifactData.TryGetValue(artifact, out var scannedData))
        {
            scannedData.CurrentNodeId = (int)currentNodeId;
            scannedData.KnownNodeIds.Add((int)currentNodeId);
            scannedData.CurrentNodeIdLastUpdated = now;

            //Get artifact trigger & effect; if already exist but different from actual node then admin intervention has happened and we
            // need to obfuscate them
            if (scannedData.Nodes.Exists(x => x.NodeId == currentNodeId))
            {
                var nodeData = scannedData.Nodes.Find(x => x.NodeId == currentNodeId);
                var artiNode = _artifact.GetNodeFromId((int)currentNodeId, artiComp);
                if (artiNode is not null)
                {
                    if (nodeData.Trigger != artiNode.Trigger || nodeData.Effect != artiNode.Effect)
                    {
                        nodeData.Trigger = "ERROR";
                        nodeData.Effect = "ERROR";
                        nodeData.LastUpdated = now;
                    }
                    nodeData.Activated = artiNode.Triggered; //Advanced node scanner can tell if its triggered
                }
            }
        }
        else
        {
            // no existing data, so make a new data and put the current node in there
            var newKnownNodes = new HashSet<int>();
            newKnownNodes.Add((int)currentNodeId);
            var newData = new AdvancedNodeScannerArtifactData((int)currentNodeId, now, newKnownNodes, new List<AdvancedNodeScannerNodeData>());
            ansComp.ScannedArtifactData.Add(artifact, newData);
        }
        // Sync data to any attached analysis consoles
        var analyzerEntity = new Entity<OldArtifactAnalyzerComponent>((EntityUid)analyzer, analyzerComponent);
        TryUploadScanDataToSonsole(ansEntity, analyzerEntity, artifact, (int)currentNodeId);
    }

    /// <summary>
    /// Full scan of artifact.
    /// </summary>
    public void TryAdvancedScanNodeFull(EntityUid analyzer)
    {
        // Analyzer must have analyzer component
        if (!TryComp<OldArtifactAnalyzerComponent>(analyzer, out var analyzerComponent))
            return;

        // Can't advanced scan without advanced scanner
        var advancedNodeScanner = analyzerComponent.AdvancedNodeScanner;
        if (advancedNodeScanner is null)
            return;

        // advanced scanner needs advanced scan component
        if (!TryComp<OldAdvancedNodeScannerComponent>(advancedNodeScanner, out var ansComp))
            return; // if this happens something has gone wrong or admin intervention has occurred

        // needs to be plugged in and close to pad and such
        var ansEntity = new Entity<OldAdvancedNodeScannerComponent>((EntityUid)advancedNodeScanner, ansComp);
        if (!CanProvideAdvancedScanning(ansEntity))
            return;

        // need an artifact to advanced scan
        var maybeArtifact = _analyzer.GetArtifactForAnalysis(analyzer);
        if (maybeArtifact is null)
            return;

        var artifact = (EntityUid)maybeArtifact;

        // artifact has to have artifact component
        if (!TryComp<ArtifactComponent>(artifact, out var artiComp))
            return; // if this happens then something has gone very wrong

        var currentNodeId = artiComp.CurrentNodeId;

        if (currentNodeId is null)
            return;


        var artiNode = _artifact.GetNodeFromId((int)currentNodeId, artiComp);
        if (artiNode is null)
            return;

        var artiNodeParent = GetParentOfNode(artiComp, artiNode);
        var artiNodeChildren = artiNode.Edges.ToList<int>();
        if (artiNodeParent is not null)
            artiNodeChildren.Remove((int)artiNodeParent);

        var now = _timing.CurTime;

        //update existing artifact data
        if (ansComp.ScannedArtifactData.TryGetValue(artifact, out var scannedData))
        {
            // Because we can trigger the advanced scan by pressing "scan" on the console if we have already scanned the node,
            // we should skip the scan if we already scanned the node and we last scanned less than a second ago (for perf reasons)
            if (scannedData.CurrentNodeId == currentNodeId
                && (scannedData.CurrentNodeIdLastUpdated - now).Duration() < ansComp.MinTimeBetweenFullAdvancedScans
                // This part of the IF statement will stop us from skipping advanced scan if we did node id scan recently.
                && scannedData.Nodes.Exists(x => x.NodeId == currentNodeId)
                && (scannedData.Nodes.Find(x => x.NodeId == currentNodeId).LastUpdated - now).Duration() < ansComp.MinTimeBetweenFullAdvancedScans)
                return;

            scannedData.CurrentNodeId = (int)currentNodeId;
            scannedData.KnownNodeIds.Add((int)currentNodeId);
            scannedData.CurrentNodeIdLastUpdated = now;
            scannedData.KnownNodeIds.UnionWith(artiNode.Edges);

            // if we don't have info yet, we need to add it. otherwise we assume we already know.
            if (!scannedData.Nodes.Exists(x => x.NodeId == currentNodeId))
            {
                scannedData.Nodes.Add(new AdvancedNodeScannerNodeData(
                    (int)currentNodeId,
                    artiNode.Depth,
                    artiNodeParent,
                    artiNodeChildren,
                    artiNode.Trigger,
                    artiNode.Effect,
                    artiNode.Triggered,
                    now
                ));
            }
            else // Update trigger, effect, triggered, and children, in case any of these have changed. There should be no valid mechanism to change parent, nodes ID, or change a node's depth.
            {
                var storedNode = scannedData.Nodes.Find(x => x.NodeId == currentNodeId);
                storedNode.Activated = artiNode.Triggered;
                storedNode.ChildIds = artiNodeChildren;
                storedNode.Trigger = artiNode.Trigger;
                storedNode.Effect = artiNode.Effect;
                storedNode.LastUpdated = now;
            }
        }
        else
        {
            // no existing data, so make a new data and put the current node in there
            var newKnownNodes = new HashSet<int>((int)currentNodeId);
            var newData = new AdvancedNodeScannerArtifactData((int)currentNodeId, now, newKnownNodes, new List<AdvancedNodeScannerNodeData>());

            //We scan the entire node
            var scannedNode = new AdvancedNodeScannerNodeData(
                (int)currentNodeId,
                artiNode.Depth,
                artiNodeParent,
                artiNodeChildren,
                artiNode.Trigger,
                artiNode.Effect,
                artiNode.Triggered,
                now
            );

            newData.Nodes.Add(scannedNode);
            ansComp.ScannedArtifactData.Add(artifact, newData);
        }
        // Sync data to any attached analysis consoles
        var analyzerEntity = new Entity<OldArtifactAnalyzerComponent>((EntityUid)analyzer, analyzerComponent);
        TryUploadScanDataToSonsole(ansEntity, analyzerEntity, artifact, (int)currentNodeId);
    }

    /// <summary>
    /// Figure out what the parent of a node is (its the edge of the node with the lowest depth)
    /// </summary>
    private int? GetParentOfNode(ArtifactComponent comp, ArtifactNode childNode)
    {
        if (childNode.Depth == 0)
            return null;
        foreach (var edge in childNode.Edges)
        {
            var node = _artifact.GetNodeFromId(edge, comp);
            if (node is null)
                continue;
            if (node.Depth < childNode.Depth)
                return node.Id;
        }
        return null;
    }

    /// <summary>
    /// Uploads scan data to console for a single node. Assumes that the console has strictly less/more outdated information.
    /// </summary>
    public void TryUploadScanDataToSonsole(Entity<OldAdvancedNodeScannerComponent> ansEntity, Entity<OldArtifactAnalyzerComponent> analyzerEntity, EntityUid artifact, int currentNodeId)
    {
        var console = analyzerEntity.Comp.Console;
        if (console is null)
            return;

        if (!TryComp<OldAnalysisConsoleComponent>((EntityUid)console, out var consoleComp))
            return;

        if (!consoleComp.ScannedArtifactData.ContainsKey(artifact))
        {
            //Console does not have artifact, sufficient to upload it
            CopyArtifactData(new KeyValuePair<EntityUid, AdvancedNodeScannerArtifactData>(artifact, ansEntity.Comp.ScannedArtifactData[artifact]), ref consoleComp.ScannedArtifactData);
            return;
        }

        var localConsoleArtifactData = consoleComp.ScannedArtifactData[artifact];
        var ansArtifactData = ansEntity.Comp.ScannedArtifactData[artifact];

        //Upload the metadata
        localConsoleArtifactData.KnownNodeIds.UnionWith(ansArtifactData.KnownNodeIds);
        localConsoleArtifactData.CurrentNodeId = ansArtifactData.CurrentNodeId;
        localConsoleArtifactData.CurrentNodeIdLastUpdated = ansArtifactData.CurrentNodeIdLastUpdated;

        if (!localConsoleArtifactData.Nodes.Exists(x => x.NodeId == currentNodeId))
        {
            // New node, so upload the whole node to console
            localConsoleArtifactData.Nodes.Add(ansArtifactData.Nodes.Find(x => x.NodeId == currentNodeId));
            consoleComp.ScannedArtifactData[artifact] = localConsoleArtifactData;
            return;
        }
        var nodeIndex = localConsoleArtifactData.Nodes.FindIndex(x => x.NodeId == currentNodeId);
        localConsoleArtifactData.Nodes[nodeIndex] = ansArtifactData.Nodes.Find(x => x.NodeId == currentNodeId) with { }; // new copy
        consoleComp.ScannedArtifactData[artifact] = localConsoleArtifactData;
    }

    /// <summary>
    /// Synchronise advanced scan data between analysis console and advanced node scanner.
    /// Full sync: only for use when connecting console to analyzer.
    /// </summary>
    public void TrySynchronizeAdvancedScanData(Entity<OldAdvancedNodeScannerComponent> ansEntity, Entity<OldArtifactAnalyzerComponent> analyzerEntity)
    {
        var console = analyzerEntity.Comp.Console;
        if (console is null)
            return;

        if (!TryComp<OldAnalysisConsoleComponent>((EntityUid)console, out var consoleComp))
            return;

        //TODO implement smart synchronisation (currently pseudocode)
        var synchedArtifacts = new List<EntityUid>();

        // Smart synchronisation.
        // 1. Copy any artifacts from console to ANS if not already present
        // 2. For artifacts present in console and scanner:
        // 2a. If either console or ANS know of a node, then both should know.
        // 2b. Update the current node to most recently scanned version
        // 2c. Copy any nodes from console to ANS if not present
        // 2d. Update all node data to most recently scanned version for each node
        // 2e. Copy all nodes in ANS's artifact data to console's artifact data, if they weren't already touched (which means they must not exist in the console)
        // 3. Remaining artifacts in scanner do not exist in console, copy over to console

        //Iterate through each artifact in console
        foreach (var consoleArtifactData in consoleComp.ScannedArtifactData)
        {
            // 1. Copy any artifacts from console to ANS if not already present
            if (!ansEntity.Comp.ScannedArtifactData.ContainsKey(consoleArtifactData.Key))
            {
                synchedArtifacts.Add(consoleArtifactData.Key);
                CopyArtifactData(consoleArtifactData, ref ansEntity.Comp.ScannedArtifactData);
                continue;
            }

            //Local copy of the data, to be put back in the dictionaries at the end of this foreach
            var consoleArtifactDataLocal = consoleArtifactData.Value;
            var ansArtifactData = ansEntity.Comp.ScannedArtifactData[consoleArtifactData.Key];



            // 2a. If either console or ANS know of a node, then both should know.
            consoleArtifactDataLocal.KnownNodeIds.UnionWith(ansArtifactData.KnownNodeIds);
            ansArtifactData.KnownNodeIds.UnionWith(consoleArtifactData.Value.KnownNodeIds);

            //2b. An artifact's 'current node' is whichever data has the most recent
            if (consoleArtifactData.Value.CurrentNodeIdLastUpdated < ansArtifactData.CurrentNodeIdLastUpdated)
                consoleArtifactDataLocal.CurrentNodeId = ansArtifactData.CurrentNodeId;
            else
                ansArtifactData.CurrentNodeId = consoleArtifactData.Value.CurrentNodeId;

            var synchedNodes = new List<int>();
            var consoleNodesToUpdate = new List<int>();
            foreach (var consoleNode in consoleArtifactDataLocal.Nodes)
            {
                synchedNodes.Add(consoleNode.NodeId);
                if (!ansArtifactData.Nodes.Exists(x => x.NodeId == consoleNode.NodeId))
                {
                    // 2c. Copy any nodes from console to ANS if not present
                    ansArtifactData.Nodes.Add(consoleNode with {});
                    continue;
                }

                // 2d. Update all node data to most recently scanned version for each node
                var ansNodeIndex = ansArtifactData.Nodes.FindIndex(x => x.NodeId == consoleNode.NodeId);
                if (consoleNode.LastUpdated < ansArtifactData.Nodes[ansNodeIndex].LastUpdated)
                    consoleNodesToUpdate.Add(consoleNode.NodeId);
                else
                {
                    ansArtifactData.Nodes[ansNodeIndex] = consoleNode;
                }
            }
            // 2d. Update all node data to most recently scanned version for each node
            foreach (var toUpdate in consoleNodesToUpdate)
            {
                var consoleNodeIndex = consoleArtifactDataLocal.Nodes.FindIndex(x => x.NodeId == toUpdate);
                consoleArtifactDataLocal.Nodes[consoleNodeIndex] = ansArtifactData.Nodes.Find(x => x.NodeId == toUpdate);
            }

            // 2e. Copy all nodes in ANS's artifact data to console's artifact data, if they weren't already touched (which means they must not exist in the console)
            foreach (var ansNode in ansArtifactData.Nodes)
            {
                if (synchedNodes.Contains(ansNode.NodeId))
                    continue;
                consoleArtifactDataLocal.Nodes.Add(ansNode);
            }

            // We've been modifying a local variable all this time, actually put it back into the dictionary
            ansEntity.Comp.ScannedArtifactData[consoleArtifactData.Key] = ansArtifactData;
            consoleComp.ScannedArtifactData[consoleArtifactData.Key] = consoleArtifactDataLocal;
        }

        // 3. Remaining artifacts in scanner do not exist in console, copy over to console
        foreach (var ansArtifactData in ansEntity.Comp.ScannedArtifactData)
        {
            if (synchedArtifacts.Contains(ansArtifactData.Key))
                continue;
            CopyArtifactData(ansArtifactData, ref consoleComp.ScannedArtifactData);
        }
    }

    /// <summary>
    /// Synchronise advanced scan data between analysis console and advanced node scanner.
    /// Full sync: only for use when connecting console to analyzer.
    /// </summary>
    public void TrySynchronizeAdvancedScanData(Entity<OldAdvancedNodeScannerComponent> ansEntity)
    {
        var analyzer = ansEntity.Comp.AnalyzerEntity;
        if (analyzer is null)
            return;

        if (!TryComp<OldArtifactAnalyzerComponent>((EntityUid)analyzer, out var analyzerComponent))
            return;

        var analyzerEntity = new Entity<OldArtifactAnalyzerComponent>((EntityUid)analyzer, analyzerComponent);

        TrySynchronizeAdvancedScanData(ansEntity, analyzerEntity);
    }

    /// <summary>
    /// Synchronise advanced scan data between analysis console and advanced node scanner.
    /// Full sync: only for use when connecting console to analyzer.
    /// </summary>
    public void TrySynchronizeAdvancedScanData(OldAnalysisConsoleComponent consoleComp)
    {
        if (consoleComp.AdvancedNodeScanner is null)
            return;
        if (!TryComp<OldAdvancedNodeScannerComponent>(consoleComp.AdvancedNodeScanner, out var ansComp))
            return;
        TrySynchronizeAdvancedScanData(new Entity<OldAdvancedNodeScannerComponent>((EntityUid)consoleComp.AdvancedNodeScanner, ansComp));
    }

    /// <summary>
    /// Can we bypass the scanning time of an artifact scanner? Yes if we have a linked advanced node scanner that has stored information on the artifact to be scanned.
    /// </summary>
    public bool AnalyzerCanInstantScan(EntityUid analyzer, EntityUid artifact)
    {
        if (!TryComp<OldArtifactAnalyzerComponent>(analyzer, out var analyzerComponent))
            return false;

        if (analyzerComponent.AdvancedNodeScanner is null)
            return false;

        var ansEntityUid = (EntityUid)analyzerComponent.AdvancedNodeScanner;

        if (!TryComp<OldAdvancedNodeScannerComponent>(ansEntityUid, out var ansComp))
            return false;

        var ansEntity = new Entity<OldAdvancedNodeScannerComponent>(ansEntityUid, ansComp);

        if (!CanProvideAdvancedScanning(ansEntity))
            return false;

        if (!TryComp<ArtifactComponent>(artifact, out var artiComp))
            return false;

        if (!ansComp.ScannedArtifactData.TryGetValue(artifact, out var artiData))
            return false;

        return artiData.Nodes.Exists(x => x.NodeId == artiComp.CurrentNodeId);

    }

    /// <summary>
    /// Deep copy of all artifact data into an AdvancedNodeScannerArtifactData dictionary (as used in OldAdvancedNodeScanner and OldAnalysisConsole components)
    /// </summary>
    public static void CopyArtifactData(KeyValuePair<EntityUid, AdvancedNodeScannerArtifactData> artifactData, ref Dictionary<EntityUid, AdvancedNodeScannerArtifactData> copyTargetDictionary)
    {
        var copiedData = new AdvancedNodeScannerArtifactData(
            artifactData.Value.CurrentNodeId,
            artifactData.Value.CurrentNodeIdLastUpdated,
            artifactData.Value.KnownNodeIds,
            new List<AdvancedNodeScannerNodeData>()
        );
        foreach (var node in artifactData.Value.Nodes)
        {
            copiedData.Nodes.Add(node with {});
        }
        copyTargetDictionary.Add(artifactData.Key, copiedData);
    }

}
