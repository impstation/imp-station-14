using Robust.Shared.GameObjects.Components.Localization;
using Content.Shared.Administration.Logs;
using Content.Shared.Hands;
using Content.Shared.Stacks;
using Content.Shared.IdentityManagement;

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
        SubscribeLocalEvent<PluralNameComponent, GotEquippedHandEvent>(OnPickup);
        SubscribeLocalEvent<PluralNameComponent, StackCountChangedEvent>(OnStackCountChanged);
    }

    private void UpdateToPlural(Entity<PluralNameComponent> uid, GrammarComponent grammar)
    {
        var someOf = Loc.GetString(uid.Comp.SomeOf);
        var meta = Comp<MetaDataComponent>(uid);
        if (uid.Comp.NameSomeOf == string.Empty) uid.Comp.NameSomeOf = meta.EntityName; //default to the entity's normal name
        uid.Comp.OverrideName = uid.Comp.NameSomeOf;
        grammar.Attributes["indefinite"] = someOf;
        //Log.Debug($"Entity {ToPrettyString(uid)} granted pretty plural descriptor");
        Dirty(uid, grammar);
    }

    private void UpdateToSingular(Entity<PluralNameComponent> uid, GrammarComponent grammar)
    {
        var oneOf = Loc.GetString(uid.Comp.OneOf, ("item", Identity.Entity(uid, EntityManager)));
        var meta = Comp<MetaDataComponent>(uid);
        if (uid.Comp.NameOneOf == string.Empty) uid.Comp.NameOneOf = meta.EntityName; //default to the entity's normal name
        uid.Comp.OverrideName = uid.Comp.NameOneOf;
        grammar.Attributes["indefinite"] = oneOf;
        //Log.Debug($"Entity {ToPrettyString(uid)} granted pretty singular descriptor");
        Dirty(uid, grammar);
    }

    private void OnPickup(Entity<PluralNameComponent> uid, ref GotEquippedHandEvent args)
    {
        var grammar = EnsureComp<GrammarComponent>(uid);
        if (TryComp<StackComponent>(uid, out var stack) && stack.Count != 1)
            UpdateToPlural(uid, grammar);
        else
            UpdateToSingular(uid, grammar); // all things are singular
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
