using Content.Shared.Anomaly.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared._Impstation.Slasher; // #Imp Add - Required for Slasher
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Anomaly;

/// <summary> System for controlling anomaly scanner device. </summary>
public abstract class SharedAnomalyScannerSystem : EntitySystem
{
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] protected readonly SharedUserInterfaceSystem UI = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnomalyScannerComponent, ScannerDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<AnomalyScannerComponent, AfterInteractEvent>(OnScannerAfterInteract);
        SubscribeLocalEvent<AnomalyShutdownEvent>(OnScannerAnomalyShutdown);
    }

    private void OnScannerAnomalyShutdown(ref AnomalyShutdownEvent args)
    {
        var query = EntityQueryEnumerator<AnomalyScannerComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.ScannedEntity != args.Anomaly) //imp edit: genericize for slasher
                continue;

            UI.CloseUi(uid, AnomalyScannerUiKey.Key);
            // Anomaly over, reset all the appearance data
            Appearance.SetData(uid, AnomalyScannerVisuals.HasAnomaly, false);
            Appearance.SetData(uid, AnomalyScannerVisuals.AnomalyIsSupercritical, false);
            Appearance.SetData(uid, AnomalyScannerVisuals.AnomalyNextPulse, 0);
            Appearance.SetData(uid, AnomalyScannerVisuals.AnomalySeverity, 0);
            Appearance.SetData(uid, AnomalyScannerVisuals.AnomalyStability, AnomalyStabilityVisuals.Stable);
        }
    }

    private void OnScannerAfterInteract(EntityUid uid, AnomalyScannerComponent component, AfterInteractEvent args)
    {
        if (args.Target is not { } target)
            return;

        //if (!HasComp<AnomalyComponent>(target))
        //       return;

        //imp edit Let other systems accept or reject this scan target via a generic event.
        var scanEv = new TryScanEntityEvent();
        RaiseLocalEvent(target, ref scanEv);

        if (scanEv.RejectionLocale != null)
        {
            Popup.PopupClient(Loc.GetString(scanEv.RejectionLocale), args.User, args.User);
            return;
        }

        if (!HasComp<AnomalyComponent>(target) && !scanEv.Accepted)
            return;

        if (!args.CanReach)
            return;

        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            args.User,
            component.ScanDoAfterDuration,
            new ScannerDoAfterEvent(),
            uid,
            target: target,
            used: uid
        )
        {
            DistanceThreshold = 2f
        };
        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    protected virtual void OnDoAfter(EntityUid uid, AnomalyScannerComponent component, DoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        Audio.PlayPredicted(component.CompleteSound, uid, args.User);
        Popup.PopupPredicted(Loc.GetString("anomaly-scanner-component-scan-complete"), uid, args.User);

        UI.OpenUi(uid, AnomalyScannerUiKey.Key, args.User);

        args.Handled = true;
    }

}

// #Imp Add - Required for Slasher
/// <summary>
/// Raised on an entity when an anomaly scanner tries to interact with it.
/// Handlers can accept the entity as a valid scan target, or reject with an optional locale popup.
/// </summary>
[ByRefEvent]
public sealed class TryScanEntityEvent
{
    /// <summary>Whether this entity was accepted as a valid scan target by a handler.</summary>
    public bool Accepted;

    /// <summary>
    /// Locale key for a rejection popup message. When set, the scan is cancelled and this popup is shown.
    /// </summary>
    public string? RejectionLocale;
}
