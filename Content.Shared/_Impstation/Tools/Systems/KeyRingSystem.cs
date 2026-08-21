using Content.Shared._Impstation.Tools.Components;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Interaction;
using Content.Shared.Lock;
using Content.Shared.Random.Helpers;
using Content.Shared.Tools.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._Impstation.Tools.Systems;

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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KeyRingComponent, AfterInteractEvent>(TryStartKeyRingDoAfter);
        SubscribeLocalEvent<KeyRingComponent, SimpleToolDoAfterEvent>(KeyRingDoAfter);
    }

    private void TryStartKeyRingDoAfter(Entity<KeyRingComponent> ent, ref AfterInteractEvent args)
    {
        if (ent.Comp.UseDelay==TimeSpan.Zero){// wanted this to be on startup but it caused a test fail
            //TODO: Replace with random predicted when we get that.
            var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, ent.GetHashCode());
            var rand = new System.Random(seed);

            ent.Comp.UseDelay = TimeSpan.FromSeconds((rand.NextDouble() % ent.Comp.MaxUseTime)+ent.Comp.MinUseTime);//system.random nextdouble doesn't have min and max args so i had to do it manually.
        }
        if (!TryComp<AccessReaderComponent>(args.Target, out var accessReader))
            return;
        if (!HasComp<DoorComponent>(args.Target) && !HasComp<LockComponent>(args.Target))
        {
            args.Handled = true;
            return;
        }
        var doargs = new DoAfterArgs(EntityManager, args.User, ent.Comp.UseDelay, new SimpleToolDoAfterEvent(), ent, target: args.Target, args.Used)
        {
            BreakOnDamage = true,
            BreakOnHandChange = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
        };

        _doAfter.TryStartDoAfter(doargs);
        args.Handled = true;

        if (!_timing.IsFirstTimePredicted)
            return;

        _audio.Stop(ent.Comp.KeyringAudioStream);
        ent.Comp.KeyringAudioStream = _audio.PlayPredicted(ent.Comp.AttemptAudio,ent,args.User)?.Entity;


    }

    private void KeyRingDoAfter(Entity<KeyRingComponent> ent, ref SimpleToolDoAfterEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;
        if (args.Target == null || args.Cancelled)//if the target somehow dissapears or the action was cancelled then return
        {
            _audio.Stop(ent.Comp.KeyringAudioStream);
            return;
        }

        var isDoor = false;
        var isLock = false;
        if(!_accessReaderSystem.GetMainAccessReader(args.Target.Value, out var accessReader))
               return;

        if (TryComp<DoorComponent>(args.Target.Value, out var doorComp))
            isDoor = true;

        if(TryComp<LockComponent>(args.Target.Value, out var lockComponent))
            isLock = true;
        if (!isDoor&& !isLock)
            return;
        var accessComponent = accessReader.Value.Comp;
        var isAirlock = HasComp<AirlockComponent>(args.Target);

        foreach (var accessList in accessComponent.AccessLists)
        {
            foreach (var accessType in ent.Comp.Blacklist)
            {
                if (!accessList.Contains(accessType))
                    continue;
                if (isDoor&&isAirlock)
                    _doorSystem.Deny(args.Target.Value, doorComp, user: args.User, predicted: true);
                return;
            }
        }

        if (isDoor && _doorSystem.TryToggleDoor(args.Target.Value, doorComp, user: args.User, predicted: true)) {
            _adminLogger.Add(LogType.Action,
                            LogImpact.Medium,
                            $"{ToPrettyString(args.User):player} used {ToPrettyString(args.Used)} on {ToPrettyString(args.Target.Value)}: {doorComp!.State}");
        }
        else if (isLock)
        {
            _lockSystem.ToggleLock(args.Target.Value, args.User, lockComponent);
            _adminLogger.Add(LogType.Action,
                LogImpact.Medium,
                $"{ToPrettyString(args.User):player} used {ToPrettyString(args.Used)} on {ToPrettyString(args.Target.Value)} locked: {lockComponent!.Locked}");
        }

        //TODO: Replace with random predicted when we get that.
        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, ent.GetHashCode());
        var rand = new System.Random(seed);

        ent.Comp.UseDelay = TimeSpan.FromSeconds((rand.NextDouble() % ent.Comp.MaxUseTime)+ent.Comp.MinUseTime);
        _audio.Stop(ent.Comp.KeyringAudioStream);
        _audio.PlayPredicted(ent.Comp.SuccessAudio, args.Target.Value, args.User);
        Dirty(ent);
    }

}
