using Content.Shared.Actions;

namespace Content.Shared._Impstation.SalvoHud;

/// <summary>
/// This handles adding the toggle salvohud actions to the player when they wear it
/// </summary>
public sealed class SharedSalvohudToggleSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ShowMaterialCompositionIconsComponent, MapInitEvent>(OnMapInitEvent);
        SubscribeLocalEvent<ShowMaterialCompositionIconsComponent, GetItemActionsEvent>(OnGetItemActions);
    }

    private void OnMapInitEvent(Entity<ShowMaterialCompositionIconsComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.ActivateActionEnt, ent.Comp.ActivateActionProtoID);
    }

    private void OnGetItemActions(Entity<ShowMaterialCompositionIconsComponent> ent, ref GetItemActionsEvent args)
    {
        args.AddAction(ref ent.Comp.ActivateActionEnt, ent.Comp.ActivateActionProtoID);
    }
}
