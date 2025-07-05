using Content.Shared.Nutrition.AnimalHusbandry;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Client.Nutrition.EntitySystems;

/// <summary>
/// This handles visuals for <see cref="InfantComponent"/>
/// </summary>
public sealed class InfantSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<InfantComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<InfantComponent, ComponentShutdown>(OnShutdown);
    }

    /// <summary>
    /// Returns false if the entity isn't an infant. Imp
    /// </summary>
    public bool IsInfant(EntityUid uid, InfantComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        return component.IsInfant;
    }

    public void SetInfant(EntityUid uid, InfantComponent component, bool infant) //imp
    {
        component.IsInfant = infant; //imp
        Dirty(uid, component); //imp
        _appearance.SetData(uid, InfantVisuals.State, infant); //imp
    }


    /// <summary>
    /// Infant 
    /// </summary>
    private void OnStartup(EntityUid uid, InfantComponent component, ComponentStartup args)
    {
        SetInfant(uid, component, component.IsInfant);
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        component.DefaultScale = sprite.Scale;
        _sprite.SetScale((uid, sprite), component.VisualScale);
    }

    /// <summary>
    /// Adult
    /// </summary>
    private void OnShutdown(EntityUid uid, InfantComponent component, ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        _sprite.SetScale((uid, sprite), component.DefaultScale);
    }

    [Serializable] //imp
    public enum InfantVisuals : byte
    {
        State
    }
}
