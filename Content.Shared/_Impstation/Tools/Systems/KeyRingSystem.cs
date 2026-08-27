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
using Robust.Shared.Timing;

namespace Content.Shared._Impstation.Tools.Systems;

public sealed partial class KeyRingSystem : EntitySystem
{
    [Dependency] private readonly SharedDoorSystem _doorSystem = default!;
    [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;
    [Dependency] private readonly LockSystem _lockSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

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

            ent.Comp.UseDelay = TimeSpan.FromSeconds(rand.NextFloat(ent.Comp.UseTime.Min, ent.Comp.UseTime.Max));
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
            BreakOnMove = true
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

        var doorComp = CompOrNull<DoorComponent>(args.Target.Value);
        var lockComp = CompOrNull<LockComponent>(args.Target.Value);

        if ((doorComp == null && lockComp == null) ||
            !_accessReaderSystem.GetMainAccessReader(args.Target.Value, out var accessReader))
            return;

        var accessComponent = accessReader.Value.Comp;

        if (_accessReaderSystem.AreAccessTagsAllowed(ent.Comp.Blacklist, accessComponent))//since were checking against a black list, if the contained tag is allowed, return.
        {
            _audio.Stop(ent.Comp.KeyringAudioStream);
            if (HasComp<AirlockComponent>(args.Target))
                _doorSystem.Deny(args.Target.Value, doorComp, user: args.User, predicted: true);
            return;
        }

        if (doorComp != null && _doorSystem.TryToggleDoor(args.Target.Value, doorComp, user: args.User, predicted: true))//Door system throws an error if you feed it a null doorcomp so we check
        {
            _adminLogger.Add(LogType.Action,
                            LogImpact.Medium,
                            $"{ToPrettyString(args.User):player} used {ToPrettyString(args.Used)} on {ToPrettyString(args.Target.Value)}: {doorComp!.State}");
        }
        else//if it's not a door it's a lock.
        {
            _lockSystem.ToggleLock(args.Target.Value, args.User, lockComp);
            _adminLogger.Add(LogType.Action,
                LogImpact.Medium,
                $"{ToPrettyString(args.User):player} used {ToPrettyString(args.Used)} on {ToPrettyString(args.Target.Value)} locked: {lockComp!.Locked}");
        }

        //TODO: Replace with random predicted when we get that.
        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, ent.GetHashCode());
        var rand = new System.Random(seed);

        ent.Comp.UseDelay = TimeSpan.FromSeconds(rand.NextFloat(ent.Comp.UseTime.Min, ent.Comp.UseTime.Max));
        _audio.Stop(ent.Comp.KeyringAudioStream);
        _audio.PlayPredicted(ent.Comp.SuccessAudio, args.Target.Value, args.User);
        Dirty(ent);
    }

}
