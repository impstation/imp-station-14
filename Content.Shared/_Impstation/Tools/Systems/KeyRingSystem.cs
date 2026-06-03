

using System.Linq;
using Content.Shared._Impstation.Tools.Components;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Interaction;
using Content.Shared.Lock;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Tools.Systems;

public sealed partial class KeyRingSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private SharedDoorSystem _doorSystem = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;

    [Dependency] protected IGameTiming Timing = default!;

    [Serializable, NetSerializable]
    public sealed partial class KeyRingDoAfterEvent : SimpleDoAfterEvent;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KeyRingComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<KeyRingComponent, UserInteractUsingEvent>(TryStartKeyCardDoAfter);
        SubscribeLocalEvent<KeyRingComponent, KeyRingDoAfterEvent>(KeyCardDoAfter);
    }

    private void OnComponentInit(Entity<KeyRingComponent> ent, ref ComponentInit args)
    {
        var accessLevels = _prototypeManager.EnumeratePrototypes<AccessLevelPrototype>();

        foreach (var access in accessLevels)
        {
            if (ent.Comp.Blacklist.Contains(access))
                continue;
            ent.Comp.KeyCards.Append(access);
        }
        ent.Comp.KeyCards.Shuffle();
    }

    private void TryStartKeyCardDoAfter(Entity<KeyRingComponent> ent, ref UserInteractUsingEvent args)
    {
        if (!TryComp<AccessReaderComponent>(args.Target, out var accessReader))
            return;

        var doargs = new DoAfterArgs(EntityManager, ent, TimeSpan.FromSeconds(5d), new KeyRingDoAfterEvent(), ent, args.Target, args.Used)
        {
            BreakOnDamage = true,
            BreakOnHandChange = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            AttemptFrequency = AttemptFrequency.StartAndEnd
        };
        _doAfter.TryStartDoAfter(doargs);
        args.Handled = true;
    }

    private void KeyCardDoAfter(Entity<KeyRingComponent> ent, ref KeyRingDoAfterEvent args)
    {
        if (!Timing.IsFirstTimePredicted)
            return;

        if (args.Target == null||args.Cancelled)//if the target somehow dissapears or the action was cancelled then return
        {
            return;
        }

    }
}
