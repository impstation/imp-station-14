using Content.Shared.Examine;
using Content.Shared.Stacks;
using Content.Shared.IdentityManagement;
using Robust.Shared.GameObjects.Components.Localization;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory.Events;
using Content.Shared.Administration.Logs;
using Robust.Shared.Containers;

namespace Content.Shared._Impstation.Examine;

/// <summary>
/// Alters the way that this object is referred to by interaction verbs, redefining its indefinite point of reference (typically "a" or "an").
/// For entities whose names on inspection should change slightly depending on how many there are.
/// Also for those whose names don't quite make sense: i.e. by default, "a glass", "a grapes", "a spesos".
///
/// NYI - Means of changing the actual (highlighted) name that appears on inspect are VERY hacky and probably touch WAY too much code. It's probably best to just leave that be.
/// </summary>
public sealed class PluralNameSystem : EntitySystem
{

    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PluralNameComponent, EntInsertedIntoContainerMessage>(OnPickup, before: new[] { typeof(SharedHandsSystem) });
        SubscribeLocalEvent<PluralNameComponent, StackCountChangedEvent>(OnStackCountChanged);
    }

    private void UpdateToPlural(Entity<PluralNameComponent> uid)
    {
        var grammar = EnsureComp<GrammarComponent>(uid);
        var someOf = Loc.GetString(uid.Comp.SomeOf);
        grammar.Attributes["indefinite"] = someOf;
        //if (!grammar.Attributes.ContainsValue(someOf))
        //{
        //    grammar.Attributes.Remove("indefinite");
        //    grammar.Attributes.TryAdd("indefinite", someOf); // define the indefinite article
        //}
        Dirty(uid, grammar);
    }

    private void UpdateToSingular(Entity<PluralNameComponent> uid)
    {
        var grammar = EnsureComp<GrammarComponent>(uid);
        var oneOf = Loc.GetString(uid.Comp.OneOf);
        grammar.Attributes["indefinite"] = oneOf;
        //if (!grammar.Attributes.ContainsValue(oneOf))
        //{
        //   grammar.Attributes.Remove("indefinite");
        //    grammar.Attributes.TryAdd("indefinite", oneOf); // define the indefinite article
        //}
        Dirty(uid, grammar);
    }

    private void OnPickup(Entity<PluralNameComponent> uid, ref EntInsertedIntoContainerMessage args)
    {
        var grammar = EnsureComp<GrammarComponent>(uid);
        if (TryComp<StackComponent>(uid, out var stack) && stack.Count != 1)
            grammar.Attributes["indefinite"] = uid.Comp.SomeOf;
        else
            grammar.Attributes["indefinite"] = uid.Comp.OneOf;

        Dirty(uid, grammar);
    }

    private void OnStackCountChanged(Entity<PluralNameComponent> uid, ref StackCountChangedEvent args)
    {
        if (args.NewCount == 1)
            UpdateToSingular(uid);
        else
            UpdateToPlural(uid);
    }
}
