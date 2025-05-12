using Content.Shared.Clothing.Components;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory.Events;
using Content.Shared.Contraband;
using System.Net;
using Content.Shared.Destructible;

namespace Content.Shared._Impstation.Obvious;

/// <summary>
/// Adds examine text to the entity that wears item, for making things obvious.
/// </summary>
public sealed class ObviousClothingSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ObviousClothingComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<ObviousClothingComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<ObviousClothingComponent, ExaminedEvent>(OnExamine);
    }

    private void OnEquipped(Entity<ObviousClothingComponent> entity, ref GotEquippedEvent args)
    {
        if (!TryComp(entity, out ClothingComponent? clothing))
            return;

        // Make sure the clothing item was equipped to the right slot, and not just held in a hand.
        var isCorrectSlot = (clothing.Slots & args.SlotFlags) != Inventory.SlotFlags.NONE;
        if (!isCorrectSlot)
            return;

        entity.Comp.Wearer = args.Equipee;
        Dirty(entity);

        //GIVE THEM INSPECT TEXT
        var obviousExamine = EnsureComp<ObviousExamineComponent>(args.Equipee);
        obviousExamine.Lines.Add(entity.Comp.ExamineOnWearer);
    }

    private void OnUnequipped(Entity<ObviousClothingComponent> entity, ref GotUnequippedEvent args)
    {
        //REMOVE THE FUCKIN INSPECT TEXT
        if (entity.Comp.Wearer == null)
            return;

        var wearer = entity.Comp.Wearer.Value;
        if (TryComp(wearer, out ObviousExamineComponent? obviousExamine))
        {
            obviousExamine.Lines.Remove(entity.Comp.ExamineOnWearer);
        }

        entity.Comp.Wearer = null;
        Dirty(entity);
    }

    private void OnExamine(Entity<ObviousClothingComponent> entity, ref ExaminedEvent args)
    {
        var contraString = "";
        var outString = Loc.GetString("obvious-on-item", ("thing", entity.Comp.Thing));
        if (TryComp(entity, out ContrabandComponent? contra))
        {
            switch (contra.Severity)
            {
                case "Syndicate":
                    contraString = Loc.GetString("obvious-on-item-contra-syndicate");
                    break;
                case "Magical":
                    contraString = Loc.GetString("obvious-on-item-contra-magical");
                    break;
                case "Major":
                    contraString = Loc.GetString("obvious-on-item-contra-major");
                    break;
                default:
                    break;
            }
            outString += " " + contraString;
        }

        args.PushMarkup(outString);
    }
}
