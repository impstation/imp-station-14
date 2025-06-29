using Content.Shared._Impstation.EntityEffects.Effects;
using Content.Shared.Movement.Systems;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Impstation.EntityEffects.Effects;

/// <summary>
/// This handles the display of dizzy effects.
/// </summary>
public sealed class DizzyVisualizerSystem : VisualizerSystem<DizzyVisualsComponent>
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

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
        if (TryComp<SpriteComponent>(ent, out _) &&
            _sprite.LayerMapTryGet(ent.Owner, DizzyVisualLayers.Dizzy, out var layer, false))
        {
            _sprite.RemoveLayer(ent.Owner, layer);
        }
    }

    private void OnComponentInit(Entity<DizzyVisualsComponent> ent, ref ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite) || !TryComp(ent, out AppearanceComponent? appearance))
            return;

        _sprite.LayerMapReserve(ent.Owner, DizzyVisualLayers.Dizzy);
        _sprite.LayerSetVisible(ent.Owner, DizzyVisualLayers.Dizzy, false);
        sprite.LayerSetShader(DizzyVisualLayers.Dizzy, "unshaded");
        // there was some stuff here but it broke & doesnt really seem necessary i will do thsi later

        UpdateAppearance(ent.Owner);
    }

    private void UpdateAppearance(Entity<SpriteComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (!_sprite.LayerMapTryGet(ent, DizzyVisualLayers.Dizzy, out var index, false))
            return;
        _sprite.LayerSetVisible(ent, index, true);
    }
}

public enum DizzyVisualLayers : byte
{
    Dizzy
}
