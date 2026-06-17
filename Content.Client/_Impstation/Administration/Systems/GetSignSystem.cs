using Content.Client._Impstation.Administration.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.Client._Impstation.Administration.Systems;

public sealed class GetSignSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<GetSignComponent, ComponentStartup>(GetSignAdded);
        SubscribeLocalEvent<GetSignComponent, ComponentShutdown>(GetSignRemoved);
    }

    private void GetSignRemoved(EntityUid uid, GetSignComponent component, ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (!sprite.LayerMapTryGet(GetSignKey.Key, out var layer))
            return;

        sprite.RemoveLayer(layer);
    }

    private void GetSignAdded(EntityUid uid, GetSignComponent component, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (sprite.LayerMapTryGet(GetSignKey.Key, out var _))
            return;

        var adj = sprite.Bounds.Height - 0.6f;

        var layer = sprite.AddLayer(new SpriteSpecifier.Rsi(new ResPath("_Impstation/Objects/Misc/getsign.rsi"), "sign"));
        sprite.LayerMapSet(GetSignKey.Key, layer);

        sprite.LayerSetOffset(layer, new Vector2(0.0f, adj));
        sprite.LayerSetShader(layer, "unshaded");
    }

    private enum GetSignKey
    {
        Key,
    }
}
