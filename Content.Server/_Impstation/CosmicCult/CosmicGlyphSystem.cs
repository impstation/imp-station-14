
using Content.Server._Impstation.CosmicCult.Components;
using Content.Server.Actions;
using Content.Server.Bible.Components;
using Content.Server.GameTicking;
using Content.Server.Popups;
using Content.Shared._Impstation.CosmicCult.Components;
using Content.Shared._Impstation.CosmicCult.Components.Examine;
using Content.Shared.Damage;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Stunnable;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Timing;

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
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CosmicGlyphComponent, ActivateInWorldEvent>(OnUseGlyph);
        SubscribeLocalEvent<CosmicGlyphConversionComponent, TryActivateGlyphEvent>(OnConversionGlyph);
        SubscribeLocalEvent<CosmicGlyphAstralProjectionComponent, TryActivateGlyphEvent>(OnAstralProjGlyph);
    }

    #region Base trigger
    private void OnUseGlyph(Entity<CosmicGlyphComponent> uid, ref ActivateInWorldEvent args)
    {
        Log.Debug($"Glyph event triggered!");
        var tgtpos = Transform(uid).Coordinates;
        var userCoords = Transform(args.User).Coordinates;
        if (args.Handled || !userCoords.TryDistance(EntityManager, tgtpos, out var distance) || distance > uid.Comp.ActivationRange || !HasComp<CosmicCultComponent>(args.User))
            return;

        args.Handled = true;

        var cultists = GatherCultists(uid, uid.Comp.ActivationRange);
        if (cultists.Count < uid.Comp.RequiredCultists)
        {
            _popup.PopupEntity(Loc.GetString("cult-glyph-not-enough-cultists"), uid, args.User);
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
        _audio.PlayPvs(uid.Comp.GylphSFX, tgtpos, AudioParams.Default.WithVolume(+1f));
        Spawn(uid.Comp.GylphVFX, tgtpos);
        QueueDel(uid);
    }
    #endregion

    #region Conversion
    private void OnConversionGlyph(Entity<CosmicGlyphConversionComponent> uid, ref TryActivateGlyphEvent args)
    {
        var possibleTargets = GetTargetsNearGlyph(uid, uid.Comp.ConversionRange, entity => HasComp<CosmicCultComponent>(entity));
        if (possibleTargets.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("cult-glyph-conditions-not-met"), uid, args.User);
            args.Cancel();
            return;
        }
        if (possibleTargets.Count >= 2)
        {
            _popup.PopupEntity(Loc.GetString("cult-glyph-too-many-targets"), uid, args.User);
            args.Cancel();
            return;
        }

        foreach (var target in possibleTargets) // FIVE GODDAMN if-statements? Yep. I know. Why? My brain doesn't have enough juice to write something more succinct.
        {
            if (_mobState.IsDead(target))
            {
                _popup.PopupEntity(Loc.GetString("cult-glyph-target-dead"), uid, args.User);
                args.Cancel();
                return;
            }
            if (uid.Comp.NegateProtection == false && HasComp<BibleUserComponent>(target))
            {
                _popup.PopupEntity(Loc.GetString("cult-glyph-target-chaplain"), uid, args.User);
                args.Cancel();
                return;
            }

            if (uid.Comp.NegateProtection == false && HasComp<MindShieldComponent>(target))
            {
                _popup.PopupEntity(Loc.GetString("cult-glyph-target-mindshield"), uid, args.User);
                args.Cancel();
                return;
            }
            else
            {
                _stun.TryStun(target, TimeSpan.FromSeconds(4f), false);
                _cultRule.CosmicConversion(target);
            }
        }
    }
    #endregion

    #region Astral Projection
    private void OnAstralProjGlyph(Entity<CosmicGlyphAstralProjectionComponent> uid, ref TryActivateGlyphEvent args)
    {
        var projectionEnt = Spawn(uid.Comp.SpawnProjection, Transform(uid).Coordinates);
        if (_mind.TryGetMind(args.User, out var mindId, out var _))
            _mind.TransferTo(mindId, projectionEnt);
        EnsureComp<CosmicMarkBlankComponent>(args.User);
        EnsureComp<CosmicAstralBodyComponent>(projectionEnt, out var astralComp);
        var mind = Comp<MindComponent>(mindId);
        mind.PreventGhosting = true;
        astralComp.OriginalBody = args.User;
        _stun.TryKnockdown(args.User, TimeSpan.FromSeconds(2), true);
    }
    #endregion

    #region Housekeeping
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
        return entities;
    }

    /// <summary>
    ///     Gets all the humanoids near a glyph.
    /// </summary>
    /// <param name="uid">The glyph.</param>
    /// <param name="range">Radius for a lookup.</param>
    /// <param name="exclude">Filter to exclude from return.</param>
    public HashSet<Entity<HumanoidAppearanceComponent>> GetTargetsNearGlyph(EntityUid uid, float range, Predicate<Entity<HumanoidAppearanceComponent>>? exclude = null)
    {
        var glyphTransform = Transform(uid);
        var possibleTargets = _lookup.GetEntitiesInRange<HumanoidAppearanceComponent>(glyphTransform.Coordinates, range);
        if (exclude != null)
            possibleTargets.RemoveWhere(exclude);

        return possibleTargets;
    }
    #endregion
}
