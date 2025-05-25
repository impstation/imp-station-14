using System.Diagnostics;
using System.Linq;
using Content.Shared._Impstation.Homunculi;
using Content.Shared._Impstation.Homunculi.Components;
using Content.Shared._Impstation.Homunculi.Incubator;
using Content.Shared.Humanoid;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._Impstation.Homunculi;

public sealed class HomunculiTypeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HomunculiTypeComponent, SharedIncubatorSystem.HomunculiColorsChangedEvent>(SetSpriteColors);
    }

    public void SetSpriteColors(Entity<HomunculiTypeComponent> ent, ref SharedIncubatorSystem.HomunculiColorsChangedEvent args)
    {
        Log.Info("Event Called");
        var homunculus = GetEntity(args.Homunculus);
        if (!TryComp<SpriteComponent>(homunculus, out var sprite))
            return;
        Log.Info("Start Layers");
        for (var i = 0; i < sprite.AllLayers.Count(); i++)
        {
            if (sprite.TryGetLayer(i, out var layer))
            {
                Log.Info("Got Layer");
                switch (layer.State.Name)
                {
                    case "skinMap" :
                        Log.Info("Set Skin");
                        sprite.LayerSetColor(i, ent.Comp.Colors.skinColor);
                        break;
                    case "eyeMap" :
                        Log.Info("Set Eye");
                        sprite.LayerSetColor(i, ent.Comp.Colors.eyeColor);
                        break;
                }
            }
        }
    }
}
