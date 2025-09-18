using Content.Shared.Actions;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Server.Xenoarchaeology.XenoArtifacts;

public partial class ArtifactSystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    [ValidatePrototypeId<EntityPrototype>] private const string ArtifactActivateActionId = "OldActionArtifactActivate";

    /// <summary>
    ///     Used to add the artifact activation action (hehe), which lets sentient artifacts activate themselves,
    ///     either through admemery or the sentience effect.
    /// </summary>
    public void InitializeActions()
    {
        SubscribeLocalEvent<ArtifactComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ArtifactComponent, ComponentRemove>(OnRemove);

        SubscribeLocalEvent<ArtifactComponent, ArtifactSelfActivateEvent>(OnSelfActivate);
    }

    private void OnMapInit(EntityUid uid, ArtifactComponent component, MapInitEvent args)
    {
        RandomizeArtifact(uid, component);
        _actions.AddAction(uid, ref component.ActivateActionEntity, ArtifactActivateActionId);
    }

    private void OnRemove(EntityUid uid, ArtifactComponent component, ComponentRemove args)
    {
        _actions.RemoveAction(uid, component.ActivateActionEntity);
    }

    private void OnSelfActivate(EntityUid uid, ArtifactComponent component, ArtifactSelfActivateEvent args)
    {
        if (component.CurrentNodeId == null)
            return;

        var curNode = GetNodeFromId(component.CurrentNodeId.Value, component).Id;
        _popup.PopupEntity(Loc.GetString("activate-artifact-popup-self", ("node", curNode)), uid, uid);
        TryActivateArtifact(uid, uid, component);

        args.Handled = true;
    }
}

/// <summary>
///     Raised as an instant action event when a sentient artifact activates itself using an action.
/// </summary>
public sealed partial class ArtifactSelfActivateEvent : InstantActionEvent
{
}
