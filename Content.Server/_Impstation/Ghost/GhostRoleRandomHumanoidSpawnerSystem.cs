using Content.Server._Impstation.Ghost.Components;
using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Humanoid.Systems;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Robust.Server.GameObjects;

namespace Content.Server._Impstation.Ghost;

/// <summary>
/// System for GhostRoleRandomHumanoidSpawnerComponent, letting ghost role spawners use RandomHumanoidSettingsPrototype.
/// </summary>
public sealed partial class GhostRoleRandomHumanoidSpawnerSystem : EntitySystem
{
    [Dependency] private readonly GhostRoleSystem _ghostRole = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly RandomHumanoidSystem _randomHumanoid = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhostRoleRandomHumanoidSpawnerComponent, TakeGhostRoleEvent>(OnSpawnerTakeRole);
    }

    private void OnSpawnerTakeRole(EntityUid uid, GhostRoleRandomHumanoidSpawnerComponent component, ref TakeGhostRoleEvent args)
    {
        if (!TryComp(uid, out GhostRoleComponent? ghostRole) ||
            !CanTakeGhost(uid, ghostRole))
        {
            args.TookRole = false;
            return;
        }

        if (component.SettingsPrototypeId == null)
            return;

        var mob = _randomHumanoid.SpawnRandomHumanoid(component.SettingsPrototypeId, Transform(uid).Coordinates, MetaData(uid).EntityName);
        _transform.AttachToGridOrMap(mob);

        var spawnedEvent = new GhostRoleSpawnerUsedEvent(uid, mob);
        RaiseLocalEvent(mob, spawnedEvent);

        if (ghostRole.MakeSentient)
            _mindSystem.MakeSentient(mob, ghostRole.AllowMovement, ghostRole.AllowSpeech);

        EnsureComp<MindContainerComponent>(mob);

        _ghostRole.GhostRoleInternalCreateMindAndTransfer(args.Player, uid, mob, ghostRole);

        QueueDel(uid);

        args.TookRole = true;
    }

    private bool CanTakeGhost(EntityUid uid, GhostRoleComponent? component = null)
    {
        return Resolve(uid, ref component, false) &&
               !component.Taken &&
               !MetaData(uid).EntityPaused;
    }
}
