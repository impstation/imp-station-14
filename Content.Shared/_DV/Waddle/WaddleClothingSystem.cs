using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Alert; //imp edit
using Content.Shared.Item.ItemToggle.Components; //imp edit
using Robust.Shared.Containers; //imp edit

namespace Content.Shared._DV.Waddle;

public sealed class WaddleClothingSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!; //imp edit
    [Dependency] private readonly SharedContainerSystem _container = default!; //imp edit, tried to avoid adding dependencies but c’est la vie
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WaddleWhenWornComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<WaddleWhenWornComponent, ClothingGotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<WaddleWhenWornComponent, ItemToggledEvent>(OnToggled); //imp edit, waddle toggling
    }

    private void OnGotEquipped(Entity<WaddleWhenWornComponent> ent, ref ClothingGotEquippedEvent args)
    {
        var user = args.Wearer;
        // imp edit, check to see if the item has the ItemToggle component.
        // if it does and it is not activated, do not add the waddling animation to the wearer.
        if (TryComp<ItemToggleComponent>(ent, out var itemToggle))
        {
            if (!itemToggle.Activated)
                return;
        }

        // imp edit, code moved to its own method
        AddWaddleAnimationComponent(ent, user);
    }

    private void OnGotUnequipped(Entity<WaddleWhenWornComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        // imp edit, code moved to its own method
        RemoveWaddleAnimationComponent(ent, args.Wearer);
    }

    // imp edit, allows waddling to be toggled through an action
    private void OnToggled(Entity<WaddleWhenWornComponent> ent, ref ItemToggledEvent args)
    {
        // copied from MagbootsSystem.cs to get the uid of the wearer. please tell me there's a more elegant way to do this.
        if (!_container.TryGetContainingContainer((ent, null, null), out var container))
            return;
        if (args.Activated)
            AddWaddleAnimationComponent(ent, container.Owner);
        else
            RemoveWaddleAnimationComponent(ent, container.Owner);
    }

    // imp edit, code block moved from OnGotEquipped to this method, since it's used in multiple methods
    private void AddWaddleAnimationComponent(Entity<WaddleWhenWornComponent> ent, EntityUid user)
    {
        // TODO: refcount
        if (EnsureComp<WaddleAnimationComponent>(user, out var waddle))
            return;

        ent.Comp.AddedWaddle = true;
        Dirty(ent);

        var comp = ent.Comp;
        if (comp.AnimationLength is {} length)
            waddle.AnimationLength = length;
        if (comp.HopIntensity is {} hopIntensity)
            waddle.HopIntensity = hopIntensity;
        if (comp.TumbleIntensity is {} tumbleIntensity)
            waddle.TumbleIntensity = tumbleIntensity;
        if (comp.RunAnimationLengthMultiplier is {} multiplier)
            waddle.RunAnimationLengthMultiplier = multiplier;

        // very unlikely that some waddle clothing doesn't change at least 1 property, don't bother doing change detection meme
        Dirty(user, waddle);
        _alerts.ShowAlert(user, ent.Comp.WaddlingAlert); //imp edit, show waddle alert
    }

    // imp edit, code block moved from OnGotUnequipped to this method, since it's used in multiple methods
    private void RemoveWaddleAnimationComponent(Entity<WaddleWhenWornComponent> ent, EntityUid user)
    {
        if (!ent.Comp.AddedWaddle)
            return;

        // TODO: refcount
        RemComp<WaddleAnimationComponent>(user);
        ent.Comp.AddedWaddle = false;
        Dirty(ent);
        _alerts.ClearAlert(user, ent.Comp.WaddlingAlert); //imp edit, clear waddle alert
    }
}
