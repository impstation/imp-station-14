using Content.Client._Wizden.Geras.Component;
using Content.Shared._Wizden.Geras;
using Robust.Client.GameObjects;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Content.Client._Wizden.Geras;

public sealed class GerasSystem : VisualizerSystem<GerasComponent>
{

    protected override void OnAppearanceChange(EntityUid uid, GerasComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
        {
            return;
        }

        if (!AppearanceSystem.TryGetData<Color>(uid, GeraColor.Color, out var color, args.Component))
        {
           return;
        }

        if (!TryComp<SpriteComponent>(uid, out var sprite))
        {
            return;
        }

        foreach (var spriteLayer in args.Sprite.AllLayers)
        {
            sprite.Color = color;
        }
    }
}
