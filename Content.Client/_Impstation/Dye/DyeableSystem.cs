using Content.Shared._Impstation.Dye;
using Content.Shared.Clothing.Components;
using Content.Shared.Item;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client._Impstation.Dye;

/// <summary>
/// Handles sprite appearance for dyed and dyeable items.
/// </summary>
public sealed class DyeableSystem : SharedDyeableSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DyedComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(Entity<DyedComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not DyedComponentState state)
            return;

        if (state.CurrentColor == ent.Comp.CurrentColor)
            return;

        // state.currentcolor will be changed by washing machines
        ent.Comp.CurrentColor = state.CurrentColor;

        UpdateSpriteComponentAppearance(ent);
        UpdateClothingComponentAppearance(ent);
        UpdateItemComponentAppearance(ent);
    }

    private void UpdateClothingComponentAppearance(Entity<DyedComponent, ClothingComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2, false))
            return;

        foreach (var slotPair in ent.Comp2.ClothingVisuals)
            foreach (var layer in ent.Comp2.ClothingVisuals[slotPair.Key])
                layer.Color = ent.Comp1.CurrentColor;
    }

    private void UpdateSpriteComponentAppearance(Entity<DyedComponent, SpriteComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2, false))
            return;

        foreach (var layer in ent.Comp2.AllLayers)
            layer.Color = ent.Comp1.CurrentColor;
    }

    private void UpdateItemComponentAppearance(Entity<DyedComponent, ItemComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2, false))
            return;

        foreach (var hand in ent.Comp2.InhandVisuals.Values)
            foreach (var layer in hand)
                layer.Color = ent.Comp1.CurrentColor;
    }
}
