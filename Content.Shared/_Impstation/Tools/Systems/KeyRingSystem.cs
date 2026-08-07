

using System.Linq;
using Content.Shared._Impstation.Tools.Components;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Audio;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Emag.Systems;
using Content.Shared.Interaction;
using Content.Shared.Lock;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Tools.Systems;

public sealed partial class KeyRingSystem : EntitySystem
{
    [Dependency] private SharedDoorSystem _doorSystem = default!;
    [Dependency] private AccessReaderSystem _accessReaderSystem = default!;
    [Dependency] private LockSystem _lockSystem = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    [Serializable, NetSerializable]
    public sealed partial class KeyRingDoorDoAfterEvent : SimpleDoAfterEvent;

    [Serializable, NetSerializable]
    public sealed partial class KeyRingLockDoAfterEvent : SimpleDoAfterEvent;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KeyRingComponent, AfterInteractEvent>(TryStartKeyCardDoAfter);
        SubscribeLocalEvent<KeyRingComponent, KeyRingDoorDoAfterEvent>(KeyCardDoorDoAfter);
        SubscribeLocalEvent<KeyRingComponent, KeyRingLockDoAfterEvent>(KeyCardLockDoAfter);
    }

    private void TryStartKeyCardDoAfter(Entity<KeyRingComponent> ent, ref AfterInteractEvent args)
    {
        if (ent.Comp.UseDelay==TimeSpan.Zero)// wanted this to be on startup but it caused a test fail
            ent.Comp.UseDelay = TimeSpan.FromSeconds(_random.NextDouble(ent.Comp.MinUseTime,ent.Comp.MaxUseTime));
        if (!TryComp<AccessReaderComponent>(args.Target, out var accessReader))
            return;

        DoAfterArgs? doargs = null;

        if (HasComp<DoorComponent>(args.Target)){
            doargs = new DoAfterArgs(EntityManager, args.User, ent.Comp.UseDelay, new KeyRingDoorDoAfterEvent(), ent, target: args.Target, args.Used)
            {
                BreakOnDamage = true,
                BreakOnHandChange = true,
                BreakOnMove = true,
                BreakOnWeightlessMove = true,
            };
        }
        else if (HasComp<LockComponent>(args.Target))
        {
            doargs = new DoAfterArgs(EntityManager, args.User, ent.Comp.UseDelay, new KeyRingLockDoAfterEvent(), ent, target: args.Target, args.Used)
            {
                BreakOnDamage = true,
                BreakOnHandChange = true,
                BreakOnMove = true,
                BreakOnWeightlessMove = true,
            };
        }
        else
        {
            args.Handled = true;
            return;
        }

        _doAfter.TryStartDoAfter(doargs);
        args.Handled = true;

        if (!_timing.IsFirstTimePredicted)
            return;

        _audio.Stop(ent.Comp.KeyringAudioStream);
        ent.Comp.KeyringAudioStream = _audio.PlayPredicted(ent.Comp.AttemptAudio,ent,args.User)?.Entity;


    }

    private void KeyCardDoorDoAfter(Entity<KeyRingComponent> ent, ref KeyRingDoorDoAfterEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;
        if (args.Target == null || args.Cancelled)//if the target somehow dissapears or the action was cancelled then return
        {
            _audio.Stop(ent.Comp.KeyringAudioStream);
            return;
        }

        if (!TryComp<DoorComponent>(args.Target.Value, out var doorComp)||!_accessReaderSystem.GetMainAccessReader(args.Target.Value, out var accessReader))
            return;

        var accessComponent = accessReader.Value.Comp;
        var isAirlock = TryComp<AirlockComponent>(args.Target, out var airlockComp);

        foreach (var accessList in accessComponent.AccessLists)
        {
            foreach (var accessType in ent.Comp.Blacklist)
            {
                if (!accessList.Contains(accessType))
                    continue;
                if (isAirlock)
                    _doorSystem.Deny(args.Target.Value, doorComp, user: args.User, predicted: true);
                return;
            }
        }

        if (_doorSystem.TryToggleDoor(args.Target.Value, doorComp, user: args.User, predicted: true)) {
            _adminLogger.Add(LogType.Action,
                            LogImpact.Medium,
                            $"{ToPrettyString(args.User):player} used {ToPrettyString(args.Used)} on {ToPrettyString(args.Target.Value)}: {doorComp.State}");
        }

        ent.Comp.UseDelay = TimeSpan.FromSeconds(_random.NextDouble(ent.Comp.MinUseTime,ent.Comp.MaxUseTime));
        _audio.Stop(ent.Comp.KeyringAudioStream);
        _audio.PlayPredicted(ent.Comp.SuccessAudio, args.Target.Value, args.User);
        Dirty(ent);
    }

    private void KeyCardLockDoAfter(Entity<KeyRingComponent> ent, ref KeyRingLockDoAfterEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;
        if (args.Target == null || args.Cancelled)//if the target somehow dissapears or the action was cancelled then return
        {
            _audio.Stop(ent.Comp.KeyringAudioStream);
            return;
        }
        if (!TryComp<LockComponent>(args.Target.Value, out var lockComponent)||!_accessReaderSystem.GetMainAccessReader(args.Target.Value, out var accessReader))
            return;

        var accessComponent = accessReader.Value.Comp;
        foreach (var accessList in accessComponent.AccessLists)
        {
            foreach (var accessType in ent.Comp.Blacklist)
            {
                if (!accessList.Contains(accessType))
                    continue;
                return;
            }
        }
        _lockSystem.ToggleLock(args.Target.Value, args.User, lockComponent);
        _adminLogger.Add(LogType.Action,
                            LogImpact.Medium,
                            $"{ToPrettyString(args.User):player} used {ToPrettyString(args.Used)} on {ToPrettyString(args.Target.Value)} locked: {lockComponent.Locked}");
        _audio.Stop(ent.Comp.KeyringAudioStream);
        _audio.PlayPredicted(ent.Comp.SuccessAudio, args.Target.Value, args.User);
        ent.Comp.UseDelay = TimeSpan.FromSeconds(_random.NextDouble(ent.Comp.MinUseTime,ent.Comp.MaxUseTime));
        Dirty(ent);
    }

}
