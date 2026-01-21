using Content.Shared._Impstation.Administration.Components;
using Content.Shared.Damage.Systems;
using Robust.Shared.Serialization;
using System.Numerics;
using Robust.Shared.Utility;
using Robust.Shared.Toolshed.Commands.Values;

namespace Content.Shared._Impstation.Administration.Systems;

public abstract class SwordDamoclesSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damage = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SwordDamoclesComponent, ComponentStartup>(OnAdded);
        SubscribeLocalEvent<SwordDamoclesComponent, ComponentShutdown>(OnRemoved);
        SubscribeLocalEvent<SwordDamoclesEvent>(OnStruck);
    }

    private void OnRemoved(Entity<SwordDamoclesComponent> ent, ref ComponentShutdown args)
    {

    }

    private void OnAdded(Entity<SwordDamoclesComponent> ent, ref ComponentStartup args)
    {

    }

    private void OnStruck(ref SwordDamoclesEvent args)
    {
        if (TryComp<SwordDamoclesComponent>(args.Victim, out var swordComp)) // if it has the component already
        {
            _damage.TryChangeDamage(args.Victim, swordComp.Damage, ignoreResistances: true, interruptsDoAfters: true); // do damage defined by the component
        }
        else // if it doesn't
        {
            EnsureComp<SwordDamoclesComponent>(args.Victim); // give it the component
        }
    }
    // TODO: Have the smite raise an event that this checks for (NOT IN OnAdded), if it doesn't exist: add it, if it does: do the damage and remove. DONE
    // TODO2: Do all the sprite stuff here. look at sharedspritesystem
}



/// <summary>
///     Is relayed when the SwordDamocles smite is used.
/// </summary>
[ByRefEvent]
public record struct SwordDamoclesEvent(EntityUid Victim); // I think this is all I need
