using Content.Shared._Impstation.Slasher.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Maths;

namespace Content.Client._Impstation.Slasher;

/// <summary>
/// Keeps the client-side effigy sprite opacity in sync with the hidden or revealed appearance state.
/// </summary>
public sealed class SlasherEffigyVisualsSystem : EntitySystem
{
    private const float HiddenEffigyOpacity = 0.5f;

    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    /// <summary>
    /// Subscribes local events and prepares dependencies for this system.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlasherEffigyComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    /// <summary>
    /// Updates effigy sprite opacity when appearance state changes between hidden and revealed.
    /// </summary>
    /// <param name="ent">Entity and effigy component data.</param>
    /// <param name="args">Appearance change event data containing sprite state.</param>
    private void OnAppearanceChange(Entity<SlasherEffigyComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_appearance.TryGetData(ent, SlasherEffigyVisuals.Status, out SlasherEffigyStatus status))
            return;

        // Hidden = dimmed so Slashers/ghosts/admins can see it's there without it being obvious to others.
        // Revealed = fully opaque, visible to everyone.
        _sprite.SetColor((ent.Owner, args.Sprite), status == SlasherEffigyStatus.Hidden
            ? Color.White.WithAlpha(HiddenEffigyOpacity)
            : Color.White);
    }
}
