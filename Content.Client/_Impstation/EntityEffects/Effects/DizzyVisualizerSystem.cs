using Content.Shared._Impstation.EntityEffects.Effects;
using Robust.Client.GameObjects;

namespace Content.Client._Impstation.EntityEffects.Effects;

/// <summary>
/// This handles the display of dizzy effects.
/// </summary>
public sealed class DizzyVisualizerSystem : VisualizerSystem<DizzyVisualsComponent>
{
    [Dependency] private readonly PointLightSystem _lights = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DizzyVisualsComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<DizzyVisualsComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<DizzyVisualsComponent> ent, ref ComponentShutdown args)
    {
        // Need LayerMapTryGet because Init fails if there's no existing sprite / appearancecomp
        // which means in some setups (most frequently no AppearanceComp) the layer never exists.
        if (TryComp<SpriteComponent>(ent, out var sprite) &&
            sprite.LayerMapTryGet(DizzyVisualLayers.Dizzy, out var layer))
        {
            sprite.RemoveLayer(layer);
        }
    }

    private void OnComponentInit(Entity<DizzyVisualsComponent> ent, ref ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite) || !TryComp(ent, out AppearanceComponent? appearance))
            return;

        sprite.LayerMapReserveBlank(DizzyVisualLayers.Dizzy);
        sprite.LayerSetVisible(DizzyVisualLayers.Dizzy, false);
        sprite.LayerSetShader(DizzyVisualLayers.Dizzy, "unshaded");
        if (ent.Comp.Sprite != null)
            sprite.LayerSetRSI(DizzyVisualLayers.Dizzy, ent.Comp.Sprite);

        UpdateAppearance(sprite);
    }

    private static void UpdateAppearance(SpriteComponent sprite)
    {
        if (!sprite.LayerMapTryGet(DizzyVisualLayers.Dizzy, out var index))
            return;
        sprite.LayerSetVisible(index, true);
    }
}

public enum DizzyVisualLayers : byte
{
    Dizzy
}
