using Content.Server.DoAfter;
using Content.Shared._Impstation.Slasher;
using Content.Shared._Impstation.Slasher.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Doors.Components;
using Content.Shared.DoAfter;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Server.Audio;

namespace Content.Server._Impstation.Slasher;

/// <summary>
/// Handles the Slasher's Rend action, which destroys a structure the player directly targets.
/// Uses EntityTargetAction so the player clicks the exact obstacle they want destroyed.
/// </summary>
public sealed class SlasherRendActionSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    /// <summary>
    /// Subscribes rend action start and do-after completion handlers.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlasherRendActionComponent, SlasherRendEvent>(OnRend);
        SubscribeLocalEvent<SlasherRendActionComponent, SlasherRendDoAfterEvent>(OnRendDoAfter);
    }

    /// <summary>
    /// Starts the rend do-after against a valid target obstacle.
    /// </summary>
    /// <param name="actionEnt">Rend action entity and component data.</param>
    /// <param name="args">Entity-target action event data.</param>
    private void OnRend(Entity<SlasherRendActionComponent> actionEnt, ref SlasherRendEvent args)
    {
        if (args.Handled)
            return;

        var obstacle = args.Target;

        if (!CanRendTarget(actionEnt.Comp, obstacle))
        {
            _popup.PopupEntity(Loc.GetString("slasher-rend-no-target"), args.Performer, args.Performer, PopupType.MediumCaution);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, args.Performer, actionEnt.Comp.RendDelay, new SlasherRendDoAfterEvent(), actionEnt, target: obstacle, used: args.Performer)
        {
            BreakOnDamage = false,
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            BreakOnHandChange = true,
            DistanceThreshold = actionEnt.Comp.DistanceThreshold,
            AttemptFrequency = AttemptFrequency.StartAndEnd,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        _audio.PlayPvs(actionEnt.Comp.RendStartSound, obstacle);
        _popup.PopupEntity(Loc.GetString("slasher-rend-start"), args.Performer, args.Performer, PopupType.LargeCaution);
        args.Handled = true;
    }

    /// <summary>
    /// Applies rend damage and completion effects when the rend do-after succeeds.
    /// </summary>
    /// <param name="actionEnt">Rend action entity and component data.</param>
    /// <param name="args">Do-after completion event data.</param>
    private void OnRendDoAfter(Entity<SlasherRendActionComponent> actionEnt, ref SlasherRendDoAfterEvent args)
    {
        if (args.Cancelled || args.Target == null)
            return;

        var obstacle = args.Target.Value;
        if (!CanRendTarget(actionEnt.Comp, obstacle))
            return;

        _damageable.TryChangeDamage(obstacle, actionEnt.Comp.RendDamage, ignoreResistances: true, origin: args.User);
        var completionSound = _tag.HasTag(obstacle, actionEnt.Comp.WindowTag)
            ? actionEnt.Comp.WindowRendCompleteSound
            : actionEnt.Comp.RendCompleteSound;

        _audio.PlayPvs(completionSound, obstacle);
        _popup.PopupEntity(Loc.GetString("slasher-rend-complete"), args.User, args.User, PopupType.LargeCaution);
    }

    /// <summary>
    /// Determines whether an entity is a valid rend target.
    /// </summary>
    /// <param name="uid">Target entity to validate.</param>
    /// <returns>True when the entity is a damageable wall, window, or door and not a mob.</returns>
    private bool CanRendTarget(SlasherRendActionComponent actionComp, EntityUid uid)
    {
        if (HasComp<MobStateComponent>(uid))
            return false;

        if (!HasComp<DamageableComponent>(uid))
            return false;

        return HasComp<DoorComponent>(uid)
               || _tag.HasTag(uid, actionComp.WallTag)
               || _tag.HasTag(uid, actionComp.WindowTag);
    }
}
