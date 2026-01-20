using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Wires;

namespace Content.Server._Impstation.Borgs.LawSync;

public sealed class LawSynchronizerSystem : EntitySystem
{
    [Dependency] private readonly LawSyncSystem _lawSync = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LawSynchronizerComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(Entity<LawSynchronizerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target is not { } target)
            return;

        args.Handled = TryLawSync(ent, target, args.User);
    }

    private bool TryLawSync(Entity<LawSynchronizerComponent> ent, EntityUid target, EntityUid user)
    {
        if (!HasComp<SiliconLawBoundComponent>(target))
            return false;

        if (TryComp<WiresPanelComponent>(target, out var panel) && !panel.Open)
        {
            _popup.PopupEntity(Loc.GetString(ent.Comp.SyncFailedWirePanelPopup), user, user);
            return false;
        }

        // If it's a valid target, sync laws - if the target has LawSynced already, force update
        if (TryComp<LawSyncedComponent>(target, out var lawSyncedComp))
            _lawSync.SyncLaws((target, lawSyncedComp));
        else
            EnsureComp<LawSyncedComponent>(target);

        _popup.PopupEntity(Loc.GetString(ent.Comp.SyncSuccessfulPopup), user, user);
        return true;
    }
}
