using Content.Server.Anomaly.Components;
using Content.Server.Anomaly.Effects;
using Content.Shared.Anomaly;
using Content.Shared.Anomaly.Components;
using Content.Shared.DoAfter;

namespace Content.Server.Anomaly;

/// <inheritdoc cref="SharedAnomalyScannerSystem"/>
public sealed class AnomalyScannerSystem : SharedAnomalyScannerSystem
{
    [Dependency] private readonly SecretDataAnomalySystem _secretData = default!;
    [Dependency] private readonly AnomalySystem _anomaly = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnomalySeverityChangedEvent>(OnScannerAnomalySeverityChanged);
        SubscribeLocalEvent<AnomalyStabilityChangedEvent>(OnScannerAnomalyStabilityChanged);
        SubscribeLocalEvent<AnomalyHealthChangedEvent>(OnScannerAnomalyHealthChanged);
        SubscribeLocalEvent<AnomalyBehaviorChangedEvent>(OnScannerAnomalyBehaviorChanged);

        Subs.BuiEvents<AnomalyScannerComponent>(
            AnomalyScannerUiKey.Key,
            subs => subs.Event<BoundUIOpenedEvent>(OnScannerUiOpened)
        );
    }

    /// <summary> Updates device with passed anomaly data. </summary>
    public void UpdateScannerWithNewScanTarget(EntityUid scanner,
        EntityUid target,
        AnomalyScannerComponent? scannerComp = null,
        AnomalyComponent? anomalyComp = null) //imp edit: EUID anomaly became EUID target to allow Slasher's effigy to work
    {
        if (!Resolve(scanner, ref scannerComp))
            return;

        scannerComp.ScannedEntity = target; // imp edit: anomaly became target to allow Slasher's effigy to work
        UpdateScannerUi(scanner, scannerComp);

        TryComp<AppearanceComponent>(scanner, out var appearanceComp);

        // imp add: Keep scanner visuals anomaly-specific. - Needed because Slasher's effigy is an "anomaly"
        if (!Resolve(target, ref anomalyComp, false))
        {
            Appearance.SetData(scanner, AnomalyScannerVisuals.HasAnomaly, false, appearanceComp);
            Appearance.SetData(scanner, AnomalyScannerVisuals.AnomalyStability, AnomalyStabilityVisuals.Stable, appearanceComp);
            Appearance.SetData(scanner, AnomalyScannerVisuals.AnomalySeverity, 0, appearanceComp);
            Appearance.SetData(scanner, AnomalyScannerVisuals.AnomalyNextPulse, 0, appearanceComp);
            return;
        }

        var anomaly = target; //end imp add
        TryComp<SecretDataAnomalyComponent>(anomaly, out var secretDataComp);

        Appearance.SetData(scanner, AnomalyScannerVisuals.HasAnomaly, true, appearanceComp);

        var stability = _secretData.IsSecret(anomaly, AnomalySecretData.Stability, secretDataComp) && !scannerComp.IgnoreSecret
            ? AnomalyStabilityVisuals.Stable
            : _anomaly.GetStabilityVisualOrStable((anomaly, anomalyComp));
        Appearance.SetData(scanner, AnomalyScannerVisuals.AnomalyStability, stability, appearanceComp);

        var severity = _secretData.IsSecret(anomaly, AnomalySecretData.Severity, secretDataComp) && !scannerComp.IgnoreSecret
            ? 0
            : anomalyComp.Severity;
        Appearance.SetData(scanner, AnomalyScannerVisuals.AnomalySeverity, severity, appearanceComp);
    }

    /// <summary>
    /// Imp add: Clears the currently scanned entity and pushes "no anomaly scanned" UI state.
    /// </summary>
    public void ClearScanTarget(EntityUid scanner, AnomalyScannerComponent? scannerComp = null)
    {
        if (!Resolve(scanner, ref scannerComp))
            return;

        scannerComp.ScannedEntity = null;
        UpdateScannerUi(scanner, scannerComp);

        TryComp<AppearanceComponent>(scanner, out var appearanceComp);
        Appearance.SetData(scanner, AnomalyScannerVisuals.HasAnomaly, false, appearanceComp);
        Appearance.SetData(scanner, AnomalyScannerVisuals.AnomalyStability, AnomalyStabilityVisuals.Stable, appearanceComp);
        Appearance.SetData(scanner, AnomalyScannerVisuals.AnomalySeverity, 0, appearanceComp);
        Appearance.SetData(scanner, AnomalyScannerVisuals.AnomalyNextPulse, 0, appearanceComp);
    }

    /// <summary> Update scanner interface. </summary>
    public void UpdateScannerUi(EntityUid uid, AnomalyScannerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        TimeSpan? nextPulse = null;
        if (TryComp<AnomalyComponent>(component.ScannedEntity, out var anomalyComponent)) // imp add: this is here because slasher's effigy is an "anomaly" so we check if it actually IS an anomaly here
            nextPulse = anomalyComponent.NextPulseTime;

        // imp add: allow non-anomaly scan targets (e.g. Slasher effigy) to supply their own countdown via ScannerBuildMessageEvent.NextPulseTime
        var msg = _anomaly.GetScannerMessage(component);
        if (nextPulse == null && component.ScannedEntity.HasValue)
        {
            var msgEv = new ScannerBuildMessageEvent();
            RaiseLocalEvent(component.ScannedEntity.Value, ref msgEv);
            nextPulse = msgEv.NextPulseTime;
        }

        var state = new AnomalyScannerUserInterfaceState(msg, nextPulse);
        UI.SetUiState(uid, AnomalyScannerUiKey.Key, state);
    }

    /// <inheritdoc />
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var anomalyQuery = EntityQueryEnumerator<AnomalyComponent>();
        while (anomalyQuery.MoveNext(out var ent, out var anomaly))
        {
            var secondsUntilNextPulse = (anomaly.NextPulseTime - Timing.CurTime).TotalSeconds;
            UpdateScannerPulseTimers((ent, anomaly),  secondsUntilNextPulse);
        }
    }

    /// <inheritdoc />
    protected override void OnDoAfter(EntityUid uid, AnomalyScannerComponent component, DoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        base.OnDoAfter(uid, component, args);

        //imp remove and replace: the below code was originally in AnomalySystem's DoAfter handler, but we need it to run even if the target isn't an anomaly
        // since Slasher's effigy is an "anomaly" but doesn't have an AnomalyComponent, this allows the scanner to update with the effigy as a target and show it
        // as intended, patch is from 109 to 116.
        //UpdateScannerWithNewAnomaly(uid, args.Args.Target.Value, component);
        var scanTarget = args.Args.Target.Value;
        var scanEv = new TryScanEntityEvent();
        RaiseLocalEvent(scanTarget, ref scanEv);

        if (!TryComp<AnomalyComponent>(scanTarget, out _) && !scanEv.Accepted)
            return;

        UpdateScannerWithNewScanTarget(uid, scanTarget, component);
    }

    private void OnScannerAnomalyHealthChanged(ref AnomalyHealthChangedEvent args)
    {
        var query = EntityQueryEnumerator<AnomalyScannerComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.ScannedEntity != args.Anomaly)  // imp add: this is here because slasher's effigy is an "anomaly"
                continue;

            UpdateScannerUi(uid, component);
        }
    }

    private void OnScannerUiOpened(EntityUid uid, AnomalyScannerComponent component, BoundUIOpenedEvent args)
    {
        UpdateScannerUi(uid, component);
    }

    private void OnScannerAnomalySeverityChanged(ref AnomalySeverityChangedEvent args)
    {
        var query = EntityQueryEnumerator<AnomalyScannerComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.ScannedEntity != args.Anomaly)  // imp add: this is here because slasher's effigy is an "anomaly"
                continue;

            var severity = _secretData.IsSecret(args.Anomaly, AnomalySecretData.Severity) && !component.IgnoreSecret ? 0 : args.Severity;

            UpdateScannerUi(uid, component);
            Appearance.SetData(uid, AnomalyScannerVisuals.AnomalySeverity, severity);
        }
    }

    private void OnScannerAnomalyStabilityChanged(ref AnomalyStabilityChangedEvent args)
    {
        var query = EntityQueryEnumerator<AnomalyScannerComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.ScannedEntity != args.Anomaly) // imp add: this is here because slasher's effigy is an "anomaly"
                continue;

            var stability = _secretData.IsSecret(args.Anomaly, AnomalySecretData.Stability) && !component.IgnoreSecret
                ? AnomalyStabilityVisuals.Stable
                : _anomaly.GetStabilityVisualOrStable(args.Anomaly);

            UpdateScannerUi(uid, component);
            Appearance.SetData(uid, AnomalyScannerVisuals.AnomalyStability, stability);
        }
    }

    private void OnScannerAnomalyBehaviorChanged(ref AnomalyBehaviorChangedEvent args)
    {
        var query = EntityQueryEnumerator<AnomalyScannerComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.ScannedEntity != args.Anomaly) // imp add: this is here because slasher's effigy is an "anomaly"
                continue;

            UpdateScannerUi(uid, component);
            // If a field becomes secret, we want to set it to 0 or stable
            // If a field becomes visible, we need to set it to the correct value, so we need to get the AnomalyComponent
            if (!TryComp<AnomalyComponent>(args.Anomaly, out var anomalyComp))
                return;

            TryComp<AppearanceComponent>(uid, out var appearanceComp);
            TryComp<SecretDataAnomalyComponent>(args.Anomaly, out var secretDataComp);

            var severity = _secretData.IsSecret(args.Anomaly, AnomalySecretData.Severity, secretDataComp) && !component.IgnoreSecret
                ? 0
                : anomalyComp.Severity;
            Appearance.SetData(uid, AnomalyScannerVisuals.AnomalySeverity, severity, appearanceComp);

            var stability = _secretData.IsSecret(args.Anomaly, AnomalySecretData.Stability, secretDataComp) && !component.IgnoreSecret
                ? AnomalyStabilityVisuals.Stable
                : _anomaly.GetStabilityVisualOrStable((args.Anomaly, anomalyComp));
            Appearance.SetData(uid, AnomalyScannerVisuals.AnomalyStability, stability, appearanceComp);
        }
    }

    private void UpdateScannerPulseTimers(Entity<AnomalyComponent> anomalyEnt, double secondsUntilNextPulse)
    {
        if (secondsUntilNextPulse > 5)
            return;

        var rounded = Math.Max(0, (int)Math.Ceiling(secondsUntilNextPulse));

        var scannerQuery = EntityQueryEnumerator<AnomalyScannerComponent>();
        while (scannerQuery.MoveNext(out var scannerUid, out var scanner))
        {
            if (scanner.ScannedEntity != anomalyEnt) // imp add: this is here because slasher's effigy is an "anomaly"
                continue;

            Appearance.SetData(scannerUid, AnomalyScannerVisuals.AnomalyNextPulse, rounded);
        }
    }
}
