using Content.Shared.Heretic;
using Content.Shared.Humanoid;
using Robust.Client.GameObjects;

namespace Content.Client.Heretic.EntitySystems;

public sealed class GhoulSystem : Shared.Heretic.EntitySystems.SharedGhoulSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GhoulComponent, ComponentStartup>(OnStartup);
    }

    public void OnStartup(EntityUid uid, GhoulComponent component, ComponentStartup args)
    {
        var ghoulColor = Color.FromHex("#505050");

        if (HasComp<HumanoidAppearanceComponent>(uid))
            return;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        foreach (var layer in sprite.AllLayers)
        {
            layer.Color = ghoulColor;
        }
    }
}
