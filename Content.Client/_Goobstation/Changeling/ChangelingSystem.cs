using Content.Client.Alerts;
using Content.Client.UserInterface.Systems.Alerts.Controls;
using Content.Shared._Goobstation.Changeling;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client._Goobstation.Changeling;

public sealed partial class GoobChangelingSystem : EntitySystem
{

    [Dependency] private readonly IPrototypeManager _prototype = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GoobChangelingComponent, UpdateAlertSpriteEvent>(OnUpdateAlert);
        SubscribeLocalEvent<GoobChangelingComponent, GetStatusIconsEvent>(GetChanglingIcon);
    }

    private void OnUpdateAlert(EntityUid uid, GoobChangelingComponent comp, ref UpdateAlertSpriteEvent args)
    {
        var stateNormalized = 0f;

        // hardcoded because uhh umm i don't know. send help.
        switch (args.Alert.AlertKey.AlertType)
        {
            case "ChangelingChemicals":
                stateNormalized = (int) (comp.Chemicals / comp.MaxChemicals * 18);
                break;

            case "ChangelingBiomass":
                stateNormalized = (int) (comp.Biomass / comp.MaxBiomass * 16);
                break;
            default:
                return;
        }
        var sprite = args.SpriteViewEnt.Comp;
        sprite.LayerSetState(AlertVisualLayers.Base, $"{stateNormalized}");
    }

    private void GetChanglingIcon(Entity<GoobChangelingComponent> ent, ref GetStatusIconsEvent args)
    {
        if (HasComp<GoobHivemindComponent>(ent) && _prototype.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
}
