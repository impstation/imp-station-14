using Content.Client.Atmos.Components;
using Content.Shared.Atmos.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Atmos.EntitySystems;

public sealed class HeatExchangerSystem : VisualizerSystem<HeatExchangerComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, HeatExchangerComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

		// Make the layer visible
        if (!AppearanceSystem.TryGetData<bool>(uid, HeatExchangerVisuals.On, out var on, args.Component))
            on = false;
        SpriteSystem.LayerSetVisible((uid, args.Sprite), HeatExchangerVisualLayers.Glow, on);

		// Set the layer's color
        if (AppearanceSystem.TryGetData<Color>(uid, HeatExchangerVisuals.Color, out var color, args.Component))
		SpriteSystem.LayerSetColor((uid, args.Sprite), HeatExchangerVisualLayers.Glow, color);

        // SpriteSystem.LayerSetVisible((uid, args.Sprite), HeatExchangerVisualLayers.Inert, !on);
        // SpriteSystem.LayerSetVisible((uid, args.Sprite), HeatExchangerVisualLayers.Glowing, on);

        // if (AppearanceSystem.TryGetData<Color>(uid, HeatExchangerVisuals.Color, out var color, args.Component))
        // {
        //     SpriteSystem.LayerSetColor((uid, args.Sprite), HeatExchangerVisualLayers.Glowing, color);
        //     SpriteSystem.LayerSetColor((uid, args.Sprite), HeatExchangerVisualLayers.Inert, color);
        // }
    }
}
