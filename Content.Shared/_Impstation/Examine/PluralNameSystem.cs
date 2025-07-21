using Content.Shared.Examine;
using Content.Shared.Stacks;
using Content.Shared.IdentityManagement;
using Robust.Shared.GameObjects.Components.Localization;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory.Events;
using Content.Shared.Administration.Logs;
using Robust.Shared.Containers;
using System.Diagnostics;

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
        SubscribeLocalEvent<PluralNameComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PluralNameComponent, StackCountChangedEvent>(OnStackCountChanged);
    }

    private void UpdateToPlural(Entity<PluralNameComponent> uid, GrammarComponent grammar)
    {
        var someOf = Loc.GetString(uid.Comp.SomeOf);
        grammar.Attributes["indefinite"] = someOf;
        //if (!grammar.Attributes.ContainsValue(someOf))
        //{
        //    grammar.Attributes.Remove("indefinite");
        //    grammar.Attributes.TryAdd("indefinite", someOf); // define the indefinite article
        //}
        Log.Debug($"Entity {ToPrettyString(uid)} granted pretty plural descriptor");
        Dirty(uid, grammar);
    }

    private void UpdateToSingular(Entity<PluralNameComponent> uid, GrammarComponent grammar)
    {
        var oneOf = Loc.GetString(uid.Comp.OneOf);
        grammar.Attributes["indefinite"] = oneOf;
        //if (!grammar.Attributes.ContainsValue(oneOf))
        //{
        //   grammar.Attributes.Remove("indefinite");
        //    grammar.Attributes.TryAdd("indefinite", oneOf); // define the indefinite article
        //}
        Log.Debug($"Entity {ToPrettyString(uid)} granted pretty singular descriptor");
        Dirty(uid, grammar);
    }

    private void OnInit(Entity<PluralNameComponent> uid, ref ComponentInit args)
    {
        UpdateToSingular(uid, EnsureComp<GrammarComponent>(uid)); // all things are singular at init
    }

    private void OnStackCountChanged(Entity<PluralNameComponent> uid, ref StackCountChangedEvent args)
    {
        var grammar = EnsureComp<GrammarComponent>(uid);
        if (args.NewCount == 1)
            UpdateToSingular(uid, grammar);
        else
            UpdateToPlural(uid, grammar);
    }
}
