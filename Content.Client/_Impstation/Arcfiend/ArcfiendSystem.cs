using Content.Client.Alerts;
using Content.Client.UserInterface.Systems.Alerts.Controls;
using Content.Shared.Arcfiend;
using Robust.Shared.Prototypes;

namespace Content.Client.Arcfiend;

public sealed partial class ArcfiendSystem : EntitySystem
{

    [Dependency] private readonly IPrototypeManager _prototype = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArcfiendComponent, UpdateAlertSpriteEvent>(OnUpdateAlert);
    }

    private void OnUpdateAlert(EntityUid uid, ArcfiendComponent comp, ref UpdateAlertSpriteEvent args)
    {
        float stateNormalized;

        if (args.Alert.AlertKey.AlertType == "ArcfiendEnergy")
        {
            stateNormalized = (int)(comp.Energy / comp.MaxEnergy * 10);
        }
        else
        {
            return;
        }
        var sprite = args.SpriteViewEnt.Comp;
        sprite.LayerSetState(AlertVisualLayers.Base, $"{stateNormalized}");
    }
}
