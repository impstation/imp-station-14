using Content.Shared.Heretic;
using Content.Shared.Humanoid;
using Content.Shared.StatusIcon.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
namespace Content.Client._Goobstation.Heretic.EntitySystems;

/// <summary>
/// Handles clientside ghoul changes - the sprite overlay and the icon
/// </summary>
public sealed class GhoulSystem : Shared.Heretic.EntitySystems.SharedGhoulSystem
{

    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GhoulComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<GhoulComponent, GetStatusIconsEvent>(GetGhoulIcon);

    }

    /// <summary>
    /// Define the ghoul's sprite color overlay and apply it unless they don't have human appearance
    /// </summary>
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

    /// <summary>
    /// Add the icon that tells heretics this entity is a ghoul
    /// </summary>
    private void GetGhoulIcon(Entity<GhoulComponent> ent, ref GetStatusIconsEvent args)
    {
        var iconPrototype = _prototype.Index(ent.Comp.StatusIcon);
        args.StatusIcons.Add(iconPrototype);
    }
}
