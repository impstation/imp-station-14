
using Content.Server._Impstation.CosmicCult.Components;
using Content.Server.Bible.Components;
using Content.Server.Popups;
using Content.Shared._Impstation.CosmicCult.Components;
using Content.Shared.Damage;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Stunnable;

namespace Content.Server._Impstation.CosmicCult;

public sealed class CosmicGlyphSystem : EntitySystem
{
    [Dependency] private readonly CosmicCultRuleSystem _cultRule = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CosmicGlyphComponent, ActivateInWorldEvent>(OnUseGlyph);
        SubscribeLocalEvent<CosmicGlyphConversionComponent, TryActivateGlyphEvent>(OnConversionGlyph);
    }

    private void OnUseGlyph(Entity<CosmicGlyphComponent> uid, ref ActivateInWorldEvent args)
    {
        Log.Debug($"Glyph event triggered!");
        var glyphCoords = Transform(uid).Coordinates;
        var userCoords = Transform(args.User).Coordinates;
        if (args.Handled || !HasComp<CosmicCultComponent>(args.User) ||
            !userCoords.TryDistance(EntityManager, glyphCoords, out var distance) ||
            distance > uid.Comp.ActivationRange)
            return;

        args.Handled = true;

        var cultists = GatherCultists(uid, uid.Comp.ActivationRange);
        if (cultists.Count < uid.Comp.RequiredCultists)
        {
            _popup.PopupEntity(Loc.GetString("cult-rune-not-enough-cultists"), uid, args.User);
            Log.Debug($"Not enough cultists.");
            return;
        }

        var tryInvokeEv = new TryActivateGlyphEvent(args.User, cultists);
        RaiseLocalEvent(uid, tryInvokeEv);
        if (tryInvokeEv.Cancelled)
            return;

        foreach (var cultist in cultists)
        {
            DealDamage(cultist, uid.Comp.ActivationDamage);
        }
        QueueDel(uid);
    }

    private void OnConversionGlyph(Entity<CosmicGlyphConversionComponent> uid, ref TryActivateGlyphEvent args)
    {
        var possibleTargets = GetTargetsNearRune(uid, uid.Comp.ConversionRange, entity => HasComp<CosmicCultComponent>(entity));
        if (possibleTargets.Count == 0)
        {
        Log.Debug($"Found 0 entities for conversion.");
            args.Cancel();
            return;
        }
        if (possibleTargets.Count > 1)
        {
        Log.Debug($"Found {possibleTargets.Count} entities! Too many, aborting.");
            args.Cancel();
            return;
        }

        foreach (var target in possibleTargets)
        {
            if (_mobState.IsDead(target))
                return;
            else
            {
                _stun.TryStun(target, TimeSpan.FromSeconds(4f), false);
                _damageable.TryChangeDamage(target, uid.Comp.ConversionHeal);
                _cultRule.TryStartCult(target);
            }
        }
    }

    private void DealDamage(EntityUid user, DamageSpecifier? damage = null)
    {
        if (damage is null)
            return;
        // So the original DamageSpecifier will not be changed.
        var newDamage = new DamageSpecifier(damage);
        _damageable.TryChangeDamage(user, newDamage, true);
    }

    /// <summary>
    ///     Gets all cultists/constructs near a glyph.
    /// </summary>
    public HashSet<EntityUid> GatherCultists(EntityUid uid, float range)
    {
        var glyphTransform = Transform(uid);
        var entities = _lookup.GetEntitiesInRange(glyphTransform.Coordinates, range);
        entities.RemoveWhere(entity => !HasComp<CosmicCultComponent>(entity));
        // entities.RemoveWhere(entity => !HasComp<CosmicCultComponent>(entity) && !HasComp<CosmicConstructComponent>(entity));
        return entities;
    }

    /// <summary>
    ///     Gets all the humanoids near a glyph.
    /// </summary>
    /// <param name="uid">The glyph.</param>
    /// <param name="range">Radius for a lookup.</param>
    /// <param name="exclude">Filter to exclude from return.</param>
    public HashSet<Entity<HumanoidAppearanceComponent>> GetTargetsNearRune(EntityUid uid, float range, Predicate<Entity<HumanoidAppearanceComponent>>? exclude = null)
    {
        var glyphTransform = Transform(uid);
        var possibleTargets = _lookup.GetEntitiesInRange<HumanoidAppearanceComponent>(glyphTransform.Coordinates, range);
        if (exclude != null)
            possibleTargets.RemoveWhere(exclude);

        return possibleTargets;
    }

}
