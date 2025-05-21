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
public sealed class WearerGetsExamineTextSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WearerGetsExamineTextComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<WearerGetsExamineTextComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<WearerGetsExamineTextComponent, ExaminedEvent>(OnExamine);
    }

    private void OnEquipped(Entity<WearerGetsExamineTextComponent> entity, ref GotEquippedEvent args)
    {
        if (!TryComp(entity, out ClothingComponent? clothing))
            return;


        if (!entity.Comp.PocketEvident) //if it's not evident in our pockets
        {
            // Make sure the clothing item was equipped to the right slot, and not just held in a hand.
            var isCorrectSlot = (clothing.Slots & args.SlotFlags) != Inventory.SlotFlags.NONE;
            if (!isCorrectSlot)
                return;
        }

        entity.Comp.Wearer = args.Equipee;
        Dirty(entity);

        //GIVE THEM INSPECT TEXT
        var obviousExamine = EnsureComp<ExtraExamineTextComponent>(args.Equipee);
        obviousExamine.Lines.TryAdd(entity.Comp.ExamineOnWearer, entity.Comp.PrefixExamineOnWearer); //using try so that we don't cause an error if we move something from slot to slot
    }

    private void OnUnequipped(Entity<WearerGetsExamineTextComponent> entity, ref GotUnequippedEvent args)
    {
        //REMOVE THE FUCKIN INSPECT TEXT
        if (entity.Comp.Wearer == null)
            return;

        var wearer = entity.Comp.Wearer.Value;
        if (TryComp(wearer, out ExtraExamineTextComponent? obviousExamine))
        {
            obviousExamine.Lines.Remove(entity.Comp.ExamineOnWearer);
        }

        entity.Comp.Wearer = null;
        Dirty(entity);
    }

    private void OnExamine(Entity<WearerGetsExamineTextComponent> entity, ref ExaminedEvent args)
    {
        var currentlyWorn = entity.Comp.Wearer != null;
        var contraString = "";
        var outString = "";
        outString = Loc.GetString(currentlyWorn ? "obvious-on-item-currently" : "obvious-on-item",
            ("used", Loc.GetString(entity.Comp.PocketEvident ? "obvious-type-pockets" : "obvious-type-default")),
            ("thing", entity.Comp.Thing),
            ("me", Identity.Entity(entity, EntityManager)));
        if (entity.Comp.WarnExamine && TryComp(entity, out ContrabandComponent? contra))
        {
            switch (contra.Severity) // apply additional text if the item is contraband to note that displaying it might be really bad
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

        if (entity.Comp.WarnExamine)
        {
            var affecting = currentlyWorn ? entity.Comp.Wearer.GetValueOrDefault() : args.Examiner;
            var prefix = Loc.GetString(entity.Comp.PrefixExamineOnWearer,
                ("user", Identity.Entity(affecting, EntityManager)),
                ("name", Identity.Name(affecting, EntityManager)));
            var examine = Loc.GetString(entity.Comp.ExamineOnWearer,
                ("user", Identity.Entity(affecting, EntityManager)),
                ("name", Identity.Name(affecting, EntityManager)));

            var warnString = Loc.GetString("obvious-on-item-for-others",
                ("will", currentlyWorn ? "can" : "will"),
                ("output", prefix + " " + examine));

            outString += "\n" + warnString;
        }

        args.PushMarkup(outString);

    }
}
