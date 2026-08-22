using Content.Server._Impstation.Monarch.Events;
using Content.Server._Impstation.Monarch.Components;
using Content.Shared._Impstation.Consume.Components;
using Content.Server.Ghost;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory;
using Content.Shared.Players;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
namespace Content.Server._Impstation.Monarch;

public sealed class MonarchRaySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly DamageableSystem _damageSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MonarchRayComponent, ClothingGotEquippedEvent>(OnClothingEquip);
        SubscribeLocalEvent<MonarchRayComponent, ClothingGotUnequippedEvent>(OnClothingUnequip);

        SubscribeLocalEvent<MonarchRayHostComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnClothingEquip(Entity<MonarchRayComponent> ent, ref ClothingGotEquippedEvent args)
    {
        EnsureComp<MonarchRayHostComponent>(args.Wearer);
        ent.Comp.CurrentWearer = args.Wearer;
    }

    private void OnClothingUnequip(Entity<MonarchRayComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        RemComp<MonarchRayHostComponent>(args.Wearer);
        ent.Comp.CurrentWearer = args.Wearer;
    }

    private void OnMobStateChanged(Entity<MonarchRayHostComponent> ent, ref MobStateChangedEvent args)
    {
        // only go off when host dies
        if (args.NewMobState != MobState.Dead)
            return;

        RemComp<MonarchRayHostComponent>(ent);

        // if host is ACTUALLY wearing grub, fire mindsteal event
        if (_inventorySystem.TryGetContainerSlotEnumerator(ent.Owner, out var enumerator, SlotFlags.HEAD))
        {
            while (enumerator.NextItem(out var item, out _))
            {
                if (!TryComp<MonarchRayComponent>(item, out var ray))
                    continue;

                OnMonarchStealMind((item, ray));
            }
        }
    }

    private void OnMonarchStealMind(Entity<MonarchRayComponent> ent)
    {
        //when this event happens, make sure the grub is equipped
        if (!TryComp<ClothingComponent>(ent, out var clothing))
            return;

        if (!clothing.Slots.HasFlag(ent.Comp.EquippedFlag))
            return;

        //prepare to transfer mind and obliterate host
        var wearer = ent.Comp.CurrentWearer;
        if (wearer == null)
            return;

        if (TryComp<ActorComponent>(wearer, out var actor) && actor.PlayerSession.GetMind() is { } mind)
        {
            _mindSystem.TransferTo(mind, ent, ghostCheckOverride: true);
        }

        // KILL
        var damage = new DamageSpecifier(_protoMan.Index(ent.Comp.WearerDamageType), ent.Comp.DamageToWearer);
        _damageSystem.TryChangeDamage(wearer.Value, damage, true);

        var soundPool = new SoundCollectionSpecifier("gib");
        _audioSystem.PlayPvs(soundPool, ent, AudioParams.Default.WithVolume(-3f));

        //she gone
        //maybe placeholder? id like a unique thing at some point
        EnsureComp<ConsumedComponent>(wearer.Value);
    }
}
