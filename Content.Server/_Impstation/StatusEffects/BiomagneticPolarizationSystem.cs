using Content.Server.Explosion.EntitySystems;
using Content.Server.Lightning;
using Content.Shared._Impstation.StatusEffectNew;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Maps;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Whitelist;
using Pidgin;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Impstation.StatusEffects;

public sealed class BiomagneticPolarizationSystem : SharedBiomagneticPolarizationSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    [Dependency] private readonly TransformSystem _xform = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;
    [Dependency] private readonly SharedPointLightSystem _lights = default!;
    [Dependency] private readonly LightningSystem _lightning = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    private readonly EntProtoId _effectID = "StatusEffectBiomagneticPolarization";
    private static readonly ProtoId<DamageTypePrototype> ShockDamage = "Shock";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BiomagneticPolarizationStatusEffectComponent, StatusEffectAppliedEvent>(OnEffectApplied);
        SubscribeLocalEvent<BiomagneticPolarizationStatusEffectComponent, StatusEffectRelayedEvent<DamageModifyEvent>>(OnDamageModified);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;

        List<EntityUid> expiredEffectEnts = [];

        var query = EntityQueryEnumerator<BiomagneticPolarizationStatusEffectComponent, StatusEffectComponent, PointLightComponent>();
        while (query.MoveNext(out var ent, out var comp, out var statusComp, out var lightComp))
        {
            if (statusComp.AppliedTo is not { } statusOwner)
                continue;

            if (comp.Expired)
            {
                expiredEffectEnts.Add(statusOwner);
                continue;
            }

            if (comp.CooldownEnd > curTime)
                continue;

            if (comp.StatusOwner is not { } physTuple)
                continue;

            HandleStaticTiles((ent, comp));

            var (dispersed, triggeredCooldown) = HandleCollisions(physTuple, comp);
            // if HandleCollisions[1] returns true, it means that the ent has collided with an entity that has the opposite polarity,
            // so we'll shoot lighting bolts, cause a modest explosion, and mark the effect as expired.
            if (dispersed)
            {
                expiredEffectEnts.Add(statusOwner);
                var arcs = _random.Next(1, 3);
                _lightning.ShootRandomLightnings(statusOwner, 5f, arcs, comp.LightningPrototype);
                _explosion.QueueExplosion(_xform.GetMapCoordinates(statusOwner), comp.ExplosionPrototype, comp.CurrentStrength * comp.ExplosionStrengthMult, 1, 100, statusOwner);
                continue;
            }
            // if HandleCollisions[2] returns true, it means that the ent has collided with an ent with different polarity and been thrown,
            // so we need to prevent collision handling for a few seconds.
            if (triggeredCooldown)
            {
                comp.CooldownEnd = curTime + comp.TriggerCooldown;
            }

            // handle strength updates - we don't want to do this every frame, so there's a cooldown.
            if (comp.NextUpdate > curTime)
                continue;
            comp.NextUpdate = curTime + comp.UpdateTime;

            // reduce strength by decay rate, then if it's 0 or less, mark this effect expired.
            comp.CurrentStrength -= comp.RealDecayRate;
            if (comp.CurrentStrength <= 0)
            {
                expiredEffectEnts.Add(statusOwner);
                continue;
            }

            // set the light proportional to the strength
            _lights.SetEnergy(ent, comp.CurrentStrength * comp.StrLightMult, lightComp);
        }

        // to avoid modifying the list while it's enumerating, we remove status effects from expired ents after the query.
        foreach (var expired in expiredEffectEnts)
        {
            _statusEffect.TryRemoveStatusEffect(expired, _effectID);
        }
        expiredEffectEnts.Clear();
    }

    private void OnEffectApplied(Entity<BiomagneticPolarizationStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        var comp = ent.Comp;

        // Set polarization randomly, 50/50. NORTH = TRUE, SOUTH = FALSE
        comp.Polarization = _random.Prob(0.5f);

        // Set light color based on polarization
        if (!TryComp<PointLightComponent>(ent, out var maybePointLight) || maybePointLight is not { } pointLight)
            return;
        var tgtColor = comp.Polarization ? comp.NorthColor : comp.SouthColor;
        _lights.SetColor(ent, tgtColor, pointLight);

        // Set the randomized decay rate
        comp.RealDecayRate = _random.NextFloat(comp.MinDecayRate, comp.MaxDecayRate);

        var physicsQuery = GetEntityQuery<PhysicsComponent>();

        if (!physicsQuery.TryComp(args.Target, out var maybePhysComp) || maybePhysComp is not { } physComp)
            return;

        comp.StatusOwner = (args.Target, physComp);
    }

    public void OnDamageModified(Entity<BiomagneticPolarizationStatusEffectComponent> ent, ref StatusEffectRelayedEvent<DamageModifyEvent> args)
    {
        foreach (var (damageType, value) in args.Args.Damage.DamageDict)
        {
            if (damageType == ShockDamage.Id)
                ent.Comp.CurrentStrength += (float)value;
        }
    }

    private void HandleStaticTiles(Entity<BiomagneticPolarizationStatusEffectComponent> ent)
    {
        HashSet<EntityUid> entities = new();
        var xform = Transform(ent);
        var maybeGridUid = xform.GridUid;
        if (maybeGridUid is not { } gridUid)
            return;
        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var tile = _mapSystem.GetTileRef(gridUid, grid, xform.Coordinates);

        if (tile != TileRef.Zero && tile != ent.Comp.LastTile)
        {
            var staticProviderOnTile = false;
            entities.Clear();
            entities = _lookup.GetEntitiesInTile(tile, LookupFlags.Dynamic | LookupFlags.Static | LookupFlags.Sundries);
            foreach (var entOnTile in entities)
            {
                if (_whitelist.IsWhitelistPass(ent.Comp.StaticElectricityProviders, entOnTile))
                    staticProviderOnTile = true;
            }
            if (staticProviderOnTile)
                ent.Comp.CurrentStrength += ent.Comp.StrProvidedByStatic;
        }
        ent.Comp.LastTile = tile;
    }
}

