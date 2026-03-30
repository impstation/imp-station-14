using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Whitelist;
using Content.Shared.Wires;

namespace Content.Server._Impstation.Borgs.LawSync;

/// <summary>
/// Handles adding/removing LawSyncedComponent to/from silicons.
/// </summary>
public sealed class LawSynchronizerSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
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
        // target must be a silicon
        if (!HasComp<SiliconLawBoundComponent>(target))
            return false;

        // target must not be on the blacklist *or* must be on the whitelist.
        var passesLists = false;
        if (ent.Comp.Blacklist != null && _whitelist.IsWhitelistFail(ent.Comp.Blacklist, target) ||
            ent.Comp.Whitelist != null && _whitelist.IsWhitelistPass(ent.Comp.Whitelist, target))
            passesLists = true;
        if (!passesLists)
        {
            _popup.PopupEntity(Loc.GetString(ent.Comp.SyncFailedIncompatiblePopup), user, user);
            return false;
        }

        // if the target has a wirepanel, the panel must be closed.
        if (TryComp<WiresPanelComponent>(target, out var panel) && !panel.Open)
        {
            _popup.PopupEntity(Loc.GetString(ent.Comp.SyncFailedWirePanelPopup), user, user);
            return false;
        }

        // If it's a valid target, sync laws - if the target has LawSynced already, remove it.
        var successText = ent.Comp.SyncSuccessfulPopup;
        if (TryComp<LawSyncedComponent>(target, out var lawSyncedComp))
        {
            RemComp(target, lawSyncedComp);
            successText = ent.Comp.DeSyncSuccessfulPopup;
        }
        else
            EnsureComp<LawSyncedComponent>(target);

        _popup.PopupEntity(Loc.GetString(successText), user, user);
        return true;
    }
}
