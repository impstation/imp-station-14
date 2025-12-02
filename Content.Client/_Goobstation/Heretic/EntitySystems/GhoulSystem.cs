using Content.Shared.Heretic;
using Content.Shared.Humanoid;
using Content.Shared.StatusIcon.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.Heretic;

public sealed class GhoulSystem : Shared.Heretic.EntitySystems.SharedGhoulSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GhoulComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<GhoulComponent, GetStatusIconsEvent>(GetGhoulIcon);
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

    private void GetGhoulIcon(Entity<GhoulComponent> ent, ref GetStatusIconsEvent args)
    {
        var iconPrototype = _prototype.Index(ent.Comp.StatusIcon);
        args.StatusIcons.Add(iconPrototype);
    }
}
