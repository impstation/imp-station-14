using Content.Shared._Impstation.CrystalMass;
using Robust.Client.GameObjects;

namespace Content.Client._Impstation.CrystalMass;

public sealed class CrystalMassVisualsSystem : VisualizerSystem<CrystalMassVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, CrystalMassVisualsComponent component, ref AppearanceChangeEvent args)
    {

        if (args.Sprite == null)
            return;
        if (AppearanceSystem.TryGetData<int>(uid, CrystalMassVisuals.Variant, out var var, args.Component))
        {
            var index = SpriteSystem.LayerMapReserve((uid, args.Sprite), $"{component.Layer}");
            SpriteSystem.LayerSetRsiState((uid, args.Sprite), index, $"crystal_cascade_{var}");
        }
    }
}
