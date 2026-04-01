using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.CombatMode;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Events;
using Content.Shared.Physics;
using Content.Shared.Stunnable;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._Impstation.Deadlock;

/// <summary>
///     yuuup im hardcoding this
/// </summary>
public sealed partial class MoKrillSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _dmg = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;

    private readonly float _abilityRange = 2f;
    private static readonly EntProtoId AbilityPulse = "EffectSparks";

    private readonly TimeSpan _burrowSpinDuration = TimeSpan.FromSeconds(1);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActionsComponent, MoKrillScornEvent>(OnScorn);

        SubscribeLocalEvent<ActionsComponent, MoKrillBurrowStartEvent>(OnBurrow);
        SubscribeLocalEvent<ActionsComponent, MoKrillBurrowEndEvent>(OnBurrowEnd);

        SubscribeLocalEvent<ActionsComponent, MoKrillSandBlastEvent>(OnSandBlast);

        SubscribeLocalEvent<ActionsComponent, MoKrillComboStartEvent>(OnComboStart);
        SubscribeLocalEvent<ActionsComponent, MoKrillComboEndEvent>(OnComboEnd);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var burrowQuery = EntityQueryEnumerator<BurrowingComponent>();
        while (burrowQuery.MoveNext(out var ent, out var comp))
        {
            if (_timing.CurTime < comp.OutTime)
                continue;
            var ev = new MoKrillBurrowEndEvent();
            RaiseLocalEvent(ent, ev);
            RemCompDeferred<BurrowingComponent>(ent);
        }

        var spinQuery = EntityQueryEnumerator<SpinningComponent>();
        while (spinQuery.MoveNext(out var ent, out var comp))
        {
            if (_timing.CurTime < comp.OutTime)
                continue;
            _appearance.SetData(ent, BurrowVisuals.VisualState, BurrowVisualState.Normal);
            RemCompDeferred<SpinningComponent>(ent);
        }

        var comboQuery = EntityQueryEnumerator<ComboingComponent>();
        while (comboQuery.MoveNext(out var ent, out var comp))
        {
            if (_timing.CurTime < comp.EndTime)
                continue;
            var ev = new MoKrillComboEndEvent();
            RaiseLocalEvent(ent, ev);
            RemCompDeferred<ComboingComponent>(ent);
        }
    }

    private void OnScorn(Entity<ActionsComponent> ent, ref MoKrillScornEvent args)
    {
        _audio.PlayPredicted(new SoundPathSpecifier("/Audio/_Impstation/Deadlock/krill_use_power1_01.ogg"), ent, ent);
        _audio.PlayPredicted(new SoundPathSpecifier("/Audio/_Impstation/Deadlock/Digger_scorn_cast.ogg"), ent, ent);

        if (_net.IsServer)
            Spawn(AbilityPulse, Transform(ent).Coordinates);

        var heal = 0f;
        var damage = new DamageSpecifier
        {
            DamageDict = new Dictionary<string, FixedPoint2>
            {
                {"Blunt", 12f},
            },
        };

        var lookup = _lookup.GetEntitiesInRange(ent, _abilityRange, LookupFlags.Dynamic);
        foreach (var mob in lookup)
        {
            if (!TryComp<DamageableComponent>(mob, out _))
                continue;

            _dmg.TryChangeDamage(mob, damage);
            heal -= 10f;
            List<EntityUid> mobList = [];
            mobList.Add(mob);
            _color.RaiseEffect(Color.Red, mobList, Filter.Pvs(mob, entityManager: EntityManager));
        }

        var healing = new DamageSpecifier
        {
            DamageDict = new Dictionary<string, FixedPoint2>
            {
                {"Brute", heal/4},
                {"Burn", heal/4},
                {"Airloss", heal/4},
                {"Tozin", heal/4},
            },
        };

        EnsureComp<DamageableComponent>(ent, out var dmg);
        _dmg.TryChangeDamage((ent, dmg), healing);
        List<EntityUid> list = [];
        list.Add(ent);
        _color.RaiseEffect(Color.Green, list, Filter.Pvs(ent, entityManager: EntityManager));
    }

    private void OnBurrow(Entity<ActionsComponent> ent, ref MoKrillBurrowStartEvent args)
    {
        _audio.PlayPredicted(new SoundPathSpecifier("/Audio/_Impstation/Deadlock/krill_use_power2_01.ogg"), ent, ent);
        _audio.PlayPredicted(new SoundPathSpecifier("/Audio/_Impstation/Deadlock/burrowing.ogg"), ent, ent);

        EnsureComp<GodmodeComponent>(ent);
        _appearance.SetData(ent, BurrowVisuals.VisualState, BurrowVisualState.Burrowing);

        var burrow = EnsureComp<BurrowingComponent>(ent);
        burrow.OutTime = _timing.CurTime + burrow.Duration;
    }

    private void OnBurrowEnd(Entity<ActionsComponent> ent, ref MoKrillBurrowEndEvent args)
    {
        RemComp<GodmodeComponent>(ent);
        _appearance.SetData(ent, BurrowVisuals.VisualState, BurrowVisualState.Spinning);

        var lookup = _lookup.GetEntitiesInRange(ent, _abilityRange, LookupFlags.Dynamic);
        foreach (var mob in lookup)
        {
            var hit = EnsureComp<BeingBurrowHitComponent>(mob);
            hit.EndTime = _timing.CurTime + _burrowSpinDuration;
        }
        _audio.PlayPredicted(new SoundPathSpecifier("/Audio/_Impstation/Deadlock/burrowend.ogg"), ent, ent);
        var spin = EnsureComp<SpinningComponent>(ent);
        spin.OutTime = _timing.CurTime + spin.Duration;
    }

    private void OnSandBlast(Entity<ActionsComponent> ent, ref MoKrillSandBlastEvent args)
    {
        if (_net.IsServer)
            Spawn(AbilityPulse, Transform(ent).Coordinates);

        _audio.PlayPredicted(new SoundPathSpecifier("/Audio/_Impstation/Deadlock/krill_use_power3_02.ogg"), ent, ent);
        _audio.PlayPredicted(new SoundPathSpecifier("/Audio/_Impstation/Deadlock/Mokrill_a3_sandblast_cast_delay.ogg"), ent, ent);

        var damage = new DamageSpecifier
        {
            DamageDict = new Dictionary<string, FixedPoint2>
            {
                {"Blunt", 10f},
            },
        };

        var lookup = _lookup.GetEntitiesInRange(ent, _abilityRange, LookupFlags.Dynamic);
        foreach (var mob in lookup)
        {
            if (!TryComp<DamageableComponent>(mob, out _))
                continue;

            _dmg.TryChangeDamage(mob, damage);
            List<EntityUid> list = [];
            list.Add(mob);
            _color.RaiseEffect(Color.Red, list, Filter.Pvs(mob, entityManager: EntityManager));

            var disarmArgs = new DisarmedEvent(mob, ent, 0);
            RaiseLocalEvent(mob, ref disarmArgs);
        }
    }

    private void OnComboStart(Entity<ActionsComponent> ent, ref MoKrillComboStartEvent args)
    {
        if (_net.IsServer)
            Spawn(AbilityPulse, Transform(ent).Coordinates);

        _audio.PlayPredicted(new SoundPathSpecifier("/Audio/_Impstation/Deadlock/krill_use_power4_08.ogg"), ent, ent);
        _audio.PlayPredicted(new SoundPathSpecifier("/Audio/_Impstation/Deadlock/combo.ogg"), ent, ent);

        var xform = Transform(args.Performer);
        // Get the tile in front of the mokrill
        var offsetValue = xform.LocalRotation.ToWorldVec();
        var coords = xform.Coordinates.Offset(offsetValue).SnapToGrid(EntityManager, _map);

        if (xform.MapID != _transform.GetMapId(coords) || !_interaction.InRangeUnobstructed(args.Performer, args.Target, range: 1000F, collisionMask: CollisionGroup.Opaque, popup: true))
            return;

        _transform.SetCoordinates(args.Target, coords);
        _transform.AttachToGridOrMap(args.Target, Transform(args.Target));

        var comboing = EnsureComp<ComboingComponent>(ent);
        comboing.EndTime = _timing.CurTime + comboing.Duration;

        EnsureComp<StunnedComponent>(args.Target);
        var comboed = EnsureComp<BeingComboedComponent>(args.Target);
        comboed.EndTime = _timing.CurTime + comboing.Duration;
    }

    private void OnComboEnd(Entity<ActionsComponent> ent, ref MoKrillComboEndEvent args)
    {
        _audio.PlayPredicted(new SoundPathSpecifier("/Audio/_Impstation/Deadlock/Mokrill_a4_combo_end_01.ogg"), ent, ent);
        RemComp<ComboingComponent>(ent);
    }
}

public sealed partial class MoKrillScornEvent : InstantActionEvent { }
public sealed partial class MoKrillBurrowStartEvent : InstantActionEvent { }
public sealed partial class MoKrillBurrowEndEvent : EntityEventArgs { }
public sealed partial class MoKrillSandBlastEvent : InstantActionEvent { }
public sealed partial class MoKrillComboStartEvent : EntityTargetActionEvent { }
public sealed partial class MoKrillComboEndEvent : EntityEventArgs { }

[Serializable, NetSerializable]

public enum BurrowVisuals : byte
{
    VisualState,
    Spin
}

[Serializable, NetSerializable]
public enum BurrowVisualState : byte
{
    Normal,
    Burrowing,
    Spinning
}

[RegisterComponent]
public sealed partial class BurrowVisualsComponent : Component { }
[RegisterComponent]
public sealed partial class BurrowingComponent : Component
{
    public TimeSpan Duration = TimeSpan.FromSeconds(5);
    public TimeSpan OutTime;
}

[RegisterComponent]
public sealed partial class SpinningComponent : Component
{
    public TimeSpan Duration = TimeSpan.FromSeconds(2.4);
    public TimeSpan OutTime;
}

[RegisterComponent]
public sealed partial class BeingBurrowHitComponent : Component
{
    public TimeSpan HitDelay = TimeSpan.FromMilliseconds(200);
    public TimeSpan NextHit;
    public TimeSpan EndTime;
    public DamageSpecifier Damage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            {"Blunt", 5f},
        },
    };
}

public sealed class BeingBurrowHitSystem : EntitySystem
{
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly DamageableSystem _dmg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BeingBurrowHitComponent>();
        while (query.MoveNext(out var ent, out var comp))
        {
            if (_timing.CurTime < comp.NextHit)
                continue;
            comp.NextHit = _timing.CurTime + comp.HitDelay;
            _dmg.TryChangeDamage(ent, comp.Damage);
            List<EntityUid> list = [];
            list.Add(ent);
            _color.RaiseEffect(Color.Red, list, Filter.Pvs(ent, entityManager: EntityManager));


            if (_timing.CurTime > comp.EndTime)
                RemCompDeferred<BeingBurrowHitComponent>(ent);
        }
    }
}

[RegisterComponent]
public sealed partial class ComboingComponent : Component
{
    public TimeSpan Duration = TimeSpan.FromSeconds(2.4);
    public TimeSpan EndTime;
}

public sealed class ComboingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ComboingComponent, ChangeDirectionAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<ComboingComponent, UpdateCanMoveEvent>(OnAttempt);
        SubscribeLocalEvent<ComboingComponent, InteractionAttemptEvent>(OnAttemptInteract);
        SubscribeLocalEvent<ComboingComponent, UseAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<ComboingComponent, AttackAttemptEvent>(OnAttempt);
    }

    private void OnAttemptInteract(Entity<ComboingComponent> ent, ref InteractionAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnAttempt(EntityUid uid, ComboingComponent _, CancellableEntityEventArgs args)
    {
        args.Cancel();
    }

}

[RegisterComponent]
public sealed partial class BeingComboedComponent : Component
{
    public TimeSpan HitDelay = TimeSpan.FromMilliseconds(400);
    public TimeSpan NextHit;
    public TimeSpan EndTime;
    public DamageSpecifier Damage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            {"Blunt", 10f},
        },
    };
}

public sealed class BeingComboedSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _dmg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BeingComboedComponent>();
        while (query.MoveNext(out var ent, out var comp))
        {
            if (_timing.CurTime < comp.NextHit)
                continue;
            comp.NextHit = _timing.CurTime + comp.HitDelay;
            _dmg.TryChangeDamage(ent, comp.Damage);
            List<EntityUid> list = [];
            list.Add(ent);
            _color.RaiseEffect(Color.Red, list, Filter.Pvs(ent, entityManager: EntityManager));

            if (_timing.CurTime > comp.EndTime)
            {
                RemCompDeferred<BeingComboedComponent>(ent);
                RemComp<StunnedComponent>(ent);
            }
        }
    }
}
