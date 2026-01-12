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

		// Make the layer visible if it should be on
        if (!AppearanceSystem.TryGetData<bool>(uid, HeatExchangerVisuals.On, out var on, args.Component))
            on = false;
        SpriteSystem.LayerSetVisible((uid, args.Sprite), HeatExchangerGlowVisualLayers.Glow, on);

		// Set the layer's color
        if (AppearanceSystem.TryGetData<Color>(uid, HeatExchangerVisuals.Color, out var color, args.Component))
		SpriteSystem.LayerSetColor((uid, args.Sprite), HeatExchangerGlowVisualLayers.Glow, color);
    }
}
