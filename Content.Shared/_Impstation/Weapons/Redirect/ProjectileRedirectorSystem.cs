using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Inventory;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Content.Shared.Examine;
using Content.Shared.Weapons.Reflect;
using Content.Shared.NPC.Systems;
using Content.Shared.NPC.Components;
using System.Linq;

namespace Content.Shared._Impstation.Weapons.Redirect;

/// <summary>
/// This handles redirecting projectiles and hitscan shots towards specific targets.
/// </summary>
public sealed class ProjectileRedirectorSystem : EntitySystem
{
    //this should DEFINITELY inherit from reflectsystem but that's sealed :<
    //the copying copyerrrrrrrr
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    public override void Initialize()
    {
        base.Initialize();

        Subs.SubscribeWithRelay<ProjectileRedirectorComponent, ProjectileReflectAttemptEvent>(OnReflectUserCollide, baseEvent: false);
        Subs.SubscribeWithRelay<ProjectileRedirectorComponent, HitScanReflectAttemptEvent>(OnReflectUserHitscan, baseEvent: false);
        SubscribeLocalEvent<ProjectileRedirectorComponent, ProjectileReflectAttemptEvent>(OnReflectCollide);
        SubscribeLocalEvent<ProjectileRedirectorComponent, HitScanReflectAttemptEvent>(OnReflectHitscan);
    }

    private void OnReflectUserCollide(Entity<ProjectileRedirectorComponent> ent, ref ProjectileReflectAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (TryReflectProjectile(ent, ent.Owner, args.ProjUid))
            args.Cancelled = true;
    }

    private void OnReflectUserHitscan(Entity<ProjectileRedirectorComponent> ent, ref HitScanReflectAttemptEvent args)
    {
        if (args.Reflected)
            return;

        if (TryReflectHitscan(ent, ent.Owner, args.Shooter, args.SourceItem, args.Direction, args.Reflective, out var dir))
        {
            args.Direction = dir.Value;
            args.Reflected = true;
        }
    }

    private void OnReflectCollide(Entity<ProjectileRedirectorComponent> ent, ref ProjectileReflectAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (TryReflectProjectile(ent, ent.Owner, args.ProjUid))
            args.Cancelled = true;
    }

    private void OnReflectHitscan(Entity<ProjectileRedirectorComponent> ent, ref HitScanReflectAttemptEvent args)
    {
        if (args.Reflected)
            return;

        if (TryReflectHitscan(ent, ent.Owner, args.Shooter, args.SourceItem, args.Direction, args.Reflective, out var dir))
        {
            args.Direction = dir.Value;
            args.Reflected = true;
        }
    }

    private bool TryReflectProjectile(Entity<ProjectileRedirectorComponent> reflector, EntityUid user, Entity<ProjectileComponent?> projectile)
    {
        if (!TryComp<ReflectiveComponent>(projectile, out var reflective) ||
            (reflector.Comp.Reflects & reflective.Reflective) == 0x0 ||
            !_toggle.IsActivated(reflector.Owner) ||
            !_random.Prob(reflector.Comp.RedirectProb) ||
            !TryComp<PhysicsComponent>(projectile, out var physics))
        {
            return false;
        }

        var existingVelocity = _physics.GetMapLinearVelocity(projectile, component: physics);
        var newDir = GetTargetVector(reflector, existingVelocity);
        var newVelocity = newDir.Normalized() * existingVelocity.Length();
        _physics.SetLinearVelocity(projectile, newVelocity, body: physics);

        var locRot = Transform(projectile).LocalRotation;
        var newRot = locRot.ToVec();
        _transform.SetLocalRotation(projectile, newRot.ToAngle());

        PlayAudioAndPopup(reflector.Comp, user);

        if (Resolve(projectile, ref projectile.Comp, false))
        {
            _adminLogger.Add(LogType.BulletHit, LogImpact.Medium, $"{ToPrettyString(user)} reflected {ToPrettyString(projectile)} from {ToPrettyString(projectile.Comp.Weapon)} shot by {projectile.Comp.Shooter}");

            projectile.Comp.Shooter = user;
            projectile.Comp.Weapon = user;
            Dirty(projectile, projectile.Comp);
        }
        else
        {
            _adminLogger.Add(LogType.BulletHit, LogImpact.Medium, $"{ToPrettyString(user)} reflected {ToPrettyString(projectile)}");
        }

        return true;
    }
    private bool TryReflectHitscan(
        Entity<ProjectileRedirectorComponent> reflector,
        EntityUid user,
        EntityUid? shooter,
        EntityUid shotSource,
        Vector2 direction,
        ReflectType hitscanReflectType,
        [NotNullWhen(true)] out Vector2? newDirection)
    {
        if ((reflector.Comp.Reflects & hitscanReflectType) == 0x0 ||
            !_toggle.IsActivated(reflector.Owner) ||
            !_random.Prob(reflector.Comp.RedirectProb))
        {
            newDirection = null;
            return false;
        }

        PlayAudioAndPopup(reflector.Comp, user);

        newDirection = GetTargetVector(reflector, direction);

        if (shooter != null)
            _adminLogger.Add(LogType.HitScanHit, LogImpact.Medium, $"{ToPrettyString(user)} reflected hitscan from {ToPrettyString(shotSource)} shot by {ToPrettyString(shooter.Value)}");
        else
            _adminLogger.Add(LogType.HitScanHit, LogImpact.Medium, $"{ToPrettyString(user)} reflected hitscan from {ToPrettyString(shotSource)}");

        return true;
    }

    private Vector2 GetTargetVector(Entity<ProjectileRedirectorComponent> reflector, Vector2 direction)
    {
        var reflectorPos = _transform.GetWorldPosition(reflector);
        HashSet<EntityUid> possibleTargets = _lookup.GetEntitiesInRange(reflector, reflector.Comp.RedirectRadius);
        HashSet<EntityUid> chosenTargets = new();
        foreach (var possibleTarget in possibleTargets)
        {
            if (TryComp<NpcFactionMemberComponent>(possibleTarget, out var _) || TryComp<ProjectileRedirectorComponent>(possibleTarget, out var _))
            {
                if (!_faction.IsMember(possibleTarget, reflector.Comp.IgnoreFaction) //not the person who presumably created the redirector
                    && _examine.InRangeUnOccluded(reflector, possibleTarget, reflector.Comp.RedirectRadius)) //can be seen by the redirector, so doesn't hit walls
                {
                    chosenTargets.Add(possibleTarget);
                }
            }
        }

        if (chosenTargets.Count > 0)
        {
            var target = chosenTargets.ElementAt(_random.Next() % chosenTargets.Count);
            var targetPos = _transform.GetWorldPosition(target);
            return (targetPos - reflectorPos);
        }

        return direction;
    }

    private void PlayAudioAndPopup(ProjectileRedirectorComponent redirect, EntityUid user)
    {
        // Can probably be changed for prediction
        if (_netManager.IsServer)
        {
            _popup.PopupEntity(Loc.GetString("reflect-shot"), user);
            _audio.PlayPvs(redirect.SoundOnRedirect, user);
        }
    }
}
