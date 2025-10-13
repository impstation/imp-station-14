using Content.Server.Audio;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Ghost;
using Content.Server.Lightning;
using Content.Server.Storage.EntitySystems;
using Content.Server.Stunnable;
using Content.Shared._Impstation.StatusEffectNew;
using Content.Shared.Audio;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.Item;
using Content.Shared.Light.Components;
using Content.Shared.Maps;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Storage.Components;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Impstation.StatusEffects;

public sealed class BiomagneticPolarizationSystem : SharedBiomagneticPolarizationSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    [Dependency] private readonly AmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly EntityStorageSystem _entStorage = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly LightningSystem _lightning = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly SharedPointLightSystem _lights = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly TransformSystem _xform = default!;

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

            comp.LastCapped = comp.Capped;

            if (comp.CooldownEnd > curTime)
                continue;

            if (comp.StatusOwner is not { } physTuple)
                continue;

            HandleStaticTiles((ent, comp));

            var (opposite, same, capInvolved) = HandleCollisions(physTuple, comp);
            // if HandleCollisions[1] returns true, it means that the ent has collided with an entity that has the opposite polarity,
            // so we'll shoot lighting bolts, cause a modest explosion, and mark the effect as expired.
            if (opposite)
            {
                expiredEffectEnts.Add(statusOwner);

                var coords = _xform.GetMapCoordinates(statusOwner);

                if (!capInvolved)
                {
                    var arcs = _random.Next(1, 3);
                    _lightning.ShootRandomLightnings(statusOwner, 5f, arcs, comp.LightningPrototype);

                    _explosion.QueueExplosion(coords, comp.ExplosionPrototype, comp.CurrentStrength * comp.ExplosionStrengthMult, 1, 100, statusOwner);
                }
                else
                {
                    var arcs = _random.Next(2, 5);
                    _lightning.ShootRandomLightnings(statusOwner, 10f, arcs, comp.LightningPrototype);

                    _explosion.QueueExplosion(coords, comp.ExplosionPrototype, comp.StrengthCap * comp.CapExplosionMult, 2, 100, statusOwner);

                    HandleCapCollisionEffects((ent, comp));

                    _audio.PlayPvs(comp.CapExplosionSound, ent);
                }
                continue;
            }
            // if HandleCollisions[2] returns true, it means that the ent has collided with an ent with different polarity and been thrown,
            // so we need to prevent collision handling for a few seconds.
            if (same)
            {
                _stun.TryKnockdown(ent, comp.TriggerCooldown);
                comp.CooldownEnd = curTime + comp.TriggerCooldown;
            }

            // handle strength updates - we don't want to do this every frame, so there's a cooldown.
            if (comp.NextUpdate > curTime)
                continue;
            comp.NextUpdate = curTime + comp.UpdateTime;

            // clamp strength to the strength cap
            if (comp.CurrentStrength > comp.StrengthCap)
                comp.CurrentStrength = comp.StrengthCap;

            // reduce strength by decay rate, then if it's 0 or less, mark this effect expired.
            comp.CurrentStrength -= comp.RealDecayRate;
            if (comp.CurrentStrength <= 0)
            {
                expiredEffectEnts.Add(statusOwner);
                continue;
            }

            var ambientComp = EnsureComp<AmbientSoundComponent>(ent);

            // mark this component as capped if it's in the cap range, and not if it's not
            var capRangeMin = comp.StrengthCap - comp.CapEffectMargin;
            if (!comp.Capped && comp.CurrentStrength >= capRangeMin)
            {
                comp.Capped = true;

                _ambientSound.SetAmbience(ent, true, ambientComp);

                Dirty(ent, comp);
            }
            else if (comp.Capped && comp.CurrentStrength < capRangeMin)
            {
                comp.Capped = false;

                _ambientSound.SetAmbience(ent, false, ambientComp);

                Dirty(ent, comp);
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
        Dirty(ent, ent.Comp);
    }

    public void OnDamageModified(Entity<BiomagneticPolarizationStatusEffectComponent> ent, ref StatusEffectRelayedEvent<DamageModifyEvent> args)
    {
        foreach (var (damageType, value) in args.Args.Damage.DamageDict)
        {
            if (damageType != ShockDamage.Id)
                continue;
            ent.Comp.CurrentStrength += (float)value / 2;
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

    /// <summary>
    /// Much of this was copied over from RevenantSystem.Abilities.
    /// </summary>
    private void HandleCapCollisionEffects(Entity<BiomagneticPolarizationStatusEffectComponent> ent)
    {
        var worldPos = _xform.GetWorldPosition(ent);

        var lookup = _lookup.GetEntitiesInRange(ent, ent.Comp.CapEffectRange);
        var entityStorage = GetEntityQuery<EntityStorageComponent>();
        var items = GetEntityQuery<ItemComponent>();
        var humanoid = GetEntityQuery<HumanoidAppearanceComponent>();
        var lights = GetEntityQuery<PoweredLightComponent>();
        var physics = GetEntityQuery<PhysicsComponent>();

        foreach (var found in lookup)
        {
            // chuck shit (including people)
            if (physics.TryGetComponent(found, out var physComp) && physComp.BodyType != BodyType.Static)
            {
                if (items.HasComponent(found) || humanoid.HasComponent(found))
                {
                    var foundPos = _xform.GetWorldPosition(found);
                    var direction = foundPos - worldPos;
                    _throwing.TryThrow(found, direction);
                }
            }

            // everything below this line has a 50% chance per entity
            if (_random.Prob(ent.Comp.CapEffectChance))
                continue;

            // open lockers and crates at random
            if (entityStorage.TryGetComponent(found, out var entStorageComp))
                _entStorage.OpenStorage(found, entStorageComp);

            // flicker lights
            if (lights.HasComponent(found))
                _ghost.DoGhostBooEvent(found);
        }
    }
}

