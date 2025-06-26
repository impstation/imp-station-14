using System.Linq;
using Content.Server.Power.Components;
using Content.Server.Research.Systems;
using Content.Shared.UserInterface;
using Content.Server.Xenoarchaeology.Equipment.Components;
using Content.Shared.Xenoarchaeology.Equipment;
using Content.Server.Xenoarchaeology.XenoArtifacts;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Audio;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Paper;
using Content.Shared.Placeable;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Research.Components;
using Content.Shared.Xenoarchaeology.Equipment.Components;
using Content.Shared.Xenoarchaeology.XenoArtifacts;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Content.Shared.Xenoarchaeology.Artifact.Components;

namespace Content.Server.Xenoarchaeology.Equipment.Systems;

/// <summary>
/// This system is used for managing the artifact analyzer as well as the analysis console.
/// It also hanadles scanning and ui updates for both systems.
/// </summary>
public sealed class ArtifactAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ArtifactSystem _artifact = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly ResearchSystem _research = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _receiver = default!;
    [Dependency] private readonly TraversalDistorterSystem _traversalDistorter = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ActiveScannedArtifactComponent, ArtifactActivatedEvent>(OnArtifactActivated);

        SubscribeLocalEvent<ActiveArtifactAnalyzerComponent, ComponentStartup>(OnAnalyzeStart);
        SubscribeLocalEvent<ActiveArtifactAnalyzerComponent, ComponentShutdown>(OnAnalyzeEnd);
        SubscribeLocalEvent<ActiveArtifactAnalyzerComponent, PowerChangedEvent>(OnPowerChanged);

        SubscribeLocalEvent<OldArtifactAnalyzerComponent, ItemPlacedEvent>(OnItemPlaced);
        SubscribeLocalEvent<OldArtifactAnalyzerComponent, ItemRemovedEvent>(OnItemRemoved);

        SubscribeLocalEvent<OldArtifactAnalyzerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<OldAnalysisConsoleComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<OldAnalysisConsoleComponent, PortDisconnectedEvent>(OnPortDisconnected);

        SubscribeLocalEvent<OldAnalysisConsoleComponent, OldAnalysisConsoleServerSelectionMessage>(OnServerSelectionMessage);
        SubscribeLocalEvent<OldAnalysisConsoleComponent, AnalysisConsoleScanButtonPressedMessage>(OnScanButton);
        SubscribeLocalEvent<OldAnalysisConsoleComponent, AnalysisConsolePrintButtonPressedMessage>(OnPrintButton);
        SubscribeLocalEvent<OldAnalysisConsoleComponent, OldAnalysisConsoleExtractButtonPressedMessage>(OnExtractButton);
        SubscribeLocalEvent<OldAnalysisConsoleComponent, AnalysisConsoleBiasButtonPressedMessage>(OnBiasButton);

        SubscribeLocalEvent<OldAnalysisConsoleComponent, ResearchClientServerSelectedMessage>((e, c, _) => UpdateUserInterface(e, c),
            after: new[] { typeof(ResearchSystem) });
        SubscribeLocalEvent<OldAnalysisConsoleComponent, ResearchClientServerDeselectedMessage>((e, c, _) => UpdateUserInterface(e, c),
            after: new[] { typeof(ResearchSystem) });
        SubscribeLocalEvent<OldAnalysisConsoleComponent, BeforeActivatableUIOpenEvent>((e, c, _) => UpdateUserInterface(e, c));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveArtifactAnalyzerComponent, OldArtifactAnalyzerComponent>();
        while (query.MoveNext(out var uid, out var active, out var scan))
        {
            if (active.AnalysisPaused)
                continue;

            if (_timing.CurTime - active.StartTime < scan.AnalysisDuration - active.AccumulatedRunTime)
                continue;

            FinishScan(uid, scan, active);
        }
    }

    /// <summary>
    /// Resets the current scan on the artifact analyzer
    /// </summary>
    /// <param name="uid">The analyzer being reset</param>
    /// <param name="component"></param>
    [PublicAPI]
    public void ResetAnalyzer(EntityUid uid, OldArtifactAnalyzerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.LastAnalyzedArtifact = null;
        component.ReadyToPrint = false;
        UpdateAnalyzerInformation(uid, component);
    }

    /// <summary>
    /// Goes through the current entities on
    /// the analyzer and returns a valid artifact
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="placer"></param>
    /// <returns></returns>
    private EntityUid? GetArtifactForAnalysis(EntityUid? uid, ItemPlacerComponent? placer = null)
    {
        if (uid == null || !Resolve(uid.Value, ref placer))
            return null;

        var maybeArtifact = placer.PlacedEntities.FirstOrNull();
        if (HasComp<ArtifactComponent>(maybeArtifact))
            return maybeArtifact;
        return null;
    }

    /// <summary>
    /// Updates the current scan information based on
    /// the last artifact that was scanned.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    private void UpdateAnalyzerInformation(EntityUid uid, OldArtifactAnalyzerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.LastAnalyzedArtifact == null)
        {
            component.LastAnalyzerPointValue = null;
            component.LastAnalyzedNode = null;
        }
        else if (TryComp<ArtifactComponent>(component.LastAnalyzedArtifact, out var artifact))
        {
            var lastNode = artifact.CurrentNodeId == null
                ? null
                : (ArtifactNode?) _artifact.GetNodeFromId(artifact.CurrentNodeId.Value, artifact).Clone();
            component.LastAnalyzedNode = lastNode;
            component.LastAnalyzerPointValue = _artifact.GetResearchPointValue(component.LastAnalyzedArtifact.Value, artifact);
        }
    }

    private void OnMapInit(EntityUid uid, OldArtifactAnalyzerComponent component, MapInitEvent args)
    {
        if (!TryComp<DeviceLinkSinkComponent>(uid, out var sink))
            return;

        foreach (var source in sink.LinkedSources)
        {
            if (!TryComp<OldAnalysisConsoleComponent>(source, out var analysis))
                continue;
            component.Console = source;
            analysis.AnalyzerEntity = uid;
            return;
        }
    }

    private void OnNewLink(EntityUid uid, OldAnalysisConsoleComponent component, NewLinkEvent args)
    {
        if (!TryComp<OldArtifactAnalyzerComponent>(args.Sink, out var analyzer))
            return;

        component.AnalyzerEntity = args.Sink;
        analyzer.Console = uid;

        UpdateUserInterface(uid, component);
    }

    private void OnPortDisconnected(EntityUid uid, OldAnalysisConsoleComponent component, PortDisconnectedEvent args)
    {
        if (args.Port == component.LinkingPort && component.AnalyzerEntity != null)
        {
            if (TryComp<OldArtifactAnalyzerComponent>(component.AnalyzerEntity, out var analyzezr))
                analyzezr.Console = null;
            component.AnalyzerEntity = null;
        }

        UpdateUserInterface(uid, component);
    }

    private void UpdateUserInterface(EntityUid uid, OldAnalysisConsoleComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        EntityUid? artifact = null;
        FormattedMessage? msg = null;
        TimeSpan? totalTime = null;
        var canScan = false;
        var canPrint = false;
        var points = 0;

        if (TryComp<OldArtifactAnalyzerComponent>(component.AnalyzerEntity, out var analyzer))
        {
            artifact = analyzer.LastAnalyzedArtifact;
            msg = GetArtifactScanMessage(analyzer);
            totalTime = analyzer.AnalysisDuration;
            if (TryComp<ItemPlacerComponent>(component.AnalyzerEntity, out var placer))
                if (placer.PlacedEntities.Count > 0 && HasComp<ArtifactComponent>(placer.PlacedEntities.FirstOrNull()))
                    canScan = true;
                else
                    canScan = false;
            canPrint = analyzer.ReadyToPrint;

            // the artifact that's actually on the scanner right now.
            if (GetArtifactForAnalysis(component.AnalyzerEntity, placer) is { } current)
                points = _artifact.GetResearchPointValue(current);
        }

        var analyzerConnected = component.AnalyzerEntity != null;
        var serverConnected = TryComp<ResearchClientComponent>(uid, out var client) && client.ConnectedToServer;

        var scanning = TryComp<ActiveArtifactAnalyzerComponent>(component.AnalyzerEntity, out var active);
        var paused = active != null ? active.AnalysisPaused : false;

        var biasDirection = BiasDirection.Up;

        if (TryComp<TraversalDistorterComponent>(component.AnalyzerEntity, out var trav))
            biasDirection = trav.BiasDirection;

        var state = new OldAnalysisConsoleUpdateState(GetNetEntity(artifact), analyzerConnected, serverConnected,
            canScan, canPrint, msg, scanning, paused, active?.StartTime, active?.AccumulatedRunTime, totalTime, points, biasDirection == BiasDirection.Down);


        _ui.SetUiState(uid, OldArtifactAnalyzerUiKey.Key, state);

        if (TryComp<ActivatableUIComponent>(uid, out var activatableUIComponent))
        {
            if (canScan)
                activatableUIComponent.Key = OldArtifactAnalyzerUiKey.Key;
            else
                activatableUIComponent.Key = ArtifactAnalyzerUiKey.Key;
            Dirty(uid, activatableUIComponent);
        }
    }

    /// <summary>
    /// opens the server selection menu.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <param name="args"></param>
    private void OnServerSelectionMessage(EntityUid uid, OldAnalysisConsoleComponent component, OldAnalysisConsoleServerSelectionMessage args)
    {
        _ui.OpenUi(uid, ResearchClientUiKey.Key, args.Actor);
    }

    /// <summary>
    /// Starts scanning the artifact.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <param name="args"></param>
    private void OnScanButton(EntityUid uid, OldAnalysisConsoleComponent component, AnalysisConsoleScanButtonPressedMessage args)
    {
        if (component.AnalyzerEntity == null)
            return;

        if (HasComp<ActiveArtifactAnalyzerComponent>(component.AnalyzerEntity))
            return;

        var ent = GetArtifactForAnalysis(component.AnalyzerEntity);
        if (ent == null)
            return;

        var activeComp = EnsureComp<ActiveArtifactAnalyzerComponent>(component.AnalyzerEntity.Value);
        activeComp.StartTime = _timing.CurTime;
        activeComp.AccumulatedRunTime = TimeSpan.Zero;
        activeComp.Artifact = ent.Value;

        if (TryComp<ApcPowerReceiverComponent>(component.AnalyzerEntity.Value, out var powa))
            activeComp.AnalysisPaused = !powa.Powered;

        var activeArtifact = EnsureComp<ActiveScannedArtifactComponent>(ent.Value);
        activeArtifact.Scanner = component.AnalyzerEntity.Value;
        UpdateUserInterface(uid, component);
    }

    private void OnPrintButton(EntityUid uid, OldAnalysisConsoleComponent component, AnalysisConsolePrintButtonPressedMessage args)
    {
        if (component.AnalyzerEntity == null)
            return;

        if (!TryComp<OldArtifactAnalyzerComponent>(component.AnalyzerEntity, out var analyzer) ||
            analyzer.LastAnalyzedNode == null ||
            analyzer.LastAnalyzerPointValue == null ||
            !analyzer.ReadyToPrint)
        {
            return;
        }
        analyzer.ReadyToPrint = false;

        var report = Spawn(component.ReportEntityId, Transform(uid).Coordinates);
        _metaSystem.SetEntityName(report, Loc.GetString("old-analysis-report-title", ("id", analyzer.LastAnalyzedNode.Id)));

        var msg = GetArtifactScanMessage(analyzer);
        if (msg == null)
            return;

        _popup.PopupEntity(Loc.GetString("old-analysis-console-print-popup"), uid);
        if (TryComp<PaperComponent>(report, out var paperComp))
            _paper.SetContent((report, paperComp), msg.ToMarkup());
        UpdateUserInterface(uid, component);
    }

    private FormattedMessage? GetArtifactScanMessage(OldArtifactAnalyzerComponent component)
    {
        var msg = new FormattedMessage();
        if (component.LastAnalyzedNode == null)
            return null;

        var n = component.LastAnalyzedNode;

        msg.AddMarkupOrThrow(Loc.GetString("old-analysis-console-info-id", ("id", n.Id)));
        msg.PushNewline();
        msg.AddMarkupOrThrow(Loc.GetString("old-analysis-console-info-depth", ("depth", n.Depth)));
        msg.PushNewline();

        var activated = n.Triggered
            ? "old-analysis-console-info-triggered-true"
            : "old-analysis-console-info-triggered-false";
        msg.AddMarkupOrThrow(Loc.GetString(activated));
        msg.PushNewline();

        msg.PushNewline();
        var needSecondNewline = false;

        var triggerProto = _prototype.Index<ArtifactTriggerPrototype>(n.Trigger);
        if (triggerProto.TriggerHint != null)
        {
            msg.AddMarkupOrThrow(Loc.GetString("old-analysis-console-info-trigger",
                ("trigger", Loc.GetString(triggerProto.TriggerHint))) + "\n");
            needSecondNewline = true;
        }

        var effectproto = _prototype.Index<ArtifactEffectPrototype>(n.Effect);
        if (effectproto.EffectHint != null)
        {
            msg.AddMarkupOrThrow(Loc.GetString("old-analysis-console-info-effect",
                ("effect", Loc.GetString(effectproto.EffectHint))) + "\n");
            needSecondNewline = true;
        }

        if (needSecondNewline)
            msg.PushNewline();

        msg.AddMarkupOrThrow(Loc.GetString("old-analysis-console-info-edges", ("edges", n.Edges.Count)));
        msg.PushNewline();

        if (component.LastAnalyzerPointValue != null)
            msg.AddMarkupOrThrow(Loc.GetString("old-analysis-console-info-value", ("value", component.LastAnalyzerPointValue)));

        return msg;
    }

    /// <summary>
    /// Extracts points from the artifact and updates the server points
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <param name="args"></param>
    private void OnExtractButton(EntityUid uid, OldAnalysisConsoleComponent component, OldAnalysisConsoleExtractButtonPressedMessage args)
    {
        if (component.AnalyzerEntity == null)
            return;

        if (!_research.TryGetClientServer(uid, out var server, out var serverComponent))
            return;

        var artifact = GetArtifactForAnalysis(component.AnalyzerEntity);
        if (artifact == null)
            return;

        var pointValue = _artifact.GetResearchPointValue(artifact.Value);

        // no new nodes triggered so nothing to add
        if (pointValue == 0)
            return;

        _research.ModifyServerPoints(server.Value, pointValue, serverComponent);
        _artifact.AdjustConsumedPoints(artifact.Value, pointValue);

        _audio.PlayPvs(component.ExtractSound, component.AnalyzerEntity.Value, AudioParams.Default.WithVolume(2f));

        _popup.PopupEntity(Loc.GetString("old-analyzer-artifact-extract-popup"),
            component.AnalyzerEntity.Value, PopupType.Large);

        UpdateUserInterface(uid, component);
    }

    private void OnBiasButton(EntityUid uid, OldAnalysisConsoleComponent component, AnalysisConsoleBiasButtonPressedMessage args)
    {
        if (component.AnalyzerEntity == null)
            return;

        if (!TryComp<TraversalDistorterComponent>(component.AnalyzerEntity, out var trav))
            return;

        if (!_traversalDistorter.SetState(component.AnalyzerEntity.Value, trav, args.IsDown))
            return;

        UpdateUserInterface(uid, component);
    }

    /// <summary>
    /// Cancels scans if the artifact changes nodes (is activated) during the scan.
    /// </summary>
    private void OnArtifactActivated(EntityUid uid, ActiveScannedArtifactComponent component, ArtifactActivatedEvent args)
    {
        CancelScan(uid);
    }

    /// <summary>
    /// Stops the current scan
    /// </summary>
    [PublicAPI]
    public void CancelScan(EntityUid artifact, ActiveScannedArtifactComponent? component = null, OldArtifactAnalyzerComponent? analyzer = null)
    {
        if (!Resolve(artifact, ref component, false))
            return;

        if (!Resolve(component.Scanner, ref analyzer))
            return;

        _audio.PlayPvs(component.ScanFailureSound, component.Scanner, AudioParams.Default.WithVolume(3f));

        RemComp<ActiveArtifactAnalyzerComponent>(component.Scanner);
        if (analyzer.Console != null)
            UpdateUserInterface(analyzer.Console.Value);

        RemCompDeferred(artifact, component);
    }

    /// <summary>
    /// Finishes the current scan.
    /// </summary>
    [PublicAPI]
    public void FinishScan(EntityUid uid, OldArtifactAnalyzerComponent? component = null, ActiveArtifactAnalyzerComponent? active = null)
    {
        if (!Resolve(uid, ref component, ref active))
            return;

        component.ReadyToPrint = true;
        _audio.PlayPvs(component.ScanFinishedSound, uid);
        component.LastAnalyzedArtifact = active.Artifact;
        UpdateAnalyzerInformation(uid, component);

        RemComp<ActiveScannedArtifactComponent>(active.Artifact);
        RemComp(uid, active);
        if (component.Console != null)
            UpdateUserInterface(component.Console.Value);
    }

    [PublicAPI]
    public void PauseScan(EntityUid uid, OldArtifactAnalyzerComponent? component = null, ActiveArtifactAnalyzerComponent? active = null)
    {
        if (!Resolve(uid, ref component, ref active) || active.AnalysisPaused)
            return;

        active.AnalysisPaused = true;
        // As we pause, we store what was already completed.
        active.AccumulatedRunTime = (_timing.CurTime - active.StartTime) + active.AccumulatedRunTime;

        if (Exists(component.Console))
            UpdateUserInterface(component.Console.Value);
    }

    [PublicAPI]
    public void ResumeScan(EntityUid uid, OldArtifactAnalyzerComponent? component = null, ActiveArtifactAnalyzerComponent? active = null)
    {
        if (!Resolve(uid, ref component, ref active) || !active.AnalysisPaused)
            return;

        active.StartTime = _timing.CurTime;
        active.AnalysisPaused = false;

        if (Exists(component.Console))
            UpdateUserInterface(component.Console.Value);
    }

    private void OnItemPlaced(Entity<OldArtifactAnalyzerComponent> ent, ref ItemPlacedEvent args)
    {

        if (ent.Comp.Console != null && Exists(ent.Comp.Console))
            UpdateUserInterface(ent.Comp.Console.Value);
        return;
    }

    private void OnItemRemoved(Entity<OldArtifactAnalyzerComponent> ent, ref ItemRemovedEvent args)
    {
        // Scanners shouldn't give permanent remove vision to an artifact, and the scanned artifact doesn't have any
        // component to track analyzers that have scanned it for removal if the artifact gets deleted.
        // So we always clear this on removal.
        ent.Comp.LastAnalyzedArtifact = null;

        // cancel the scan if the artifact moves off the analyzer
        CancelScan(args.OtherEntity);
        if (Exists(ent.Comp.Console))
            UpdateUserInterface(ent.Comp.Console.Value);
    }

    private void OnAnalyzeStart(EntityUid uid, ActiveArtifactAnalyzerComponent component, ComponentStartup args)
    {
        _receiver.SetNeedsPower(uid, true);
        _ambientSound.SetAmbience(uid, true);
    }

    private void OnAnalyzeEnd(EntityUid uid, ActiveArtifactAnalyzerComponent component, ComponentShutdown args)
    {
        _receiver.SetNeedsPower(uid, false);
        _ambientSound.SetAmbience(uid, false);
    }

    private void OnPowerChanged(EntityUid uid, ActiveArtifactAnalyzerComponent active, ref PowerChangedEvent args)
    {
        if (!args.Powered)
        {
            PauseScan(uid, null, active);
        }
        else
        {
            ResumeScan(uid, null, active);
        }
    }
}