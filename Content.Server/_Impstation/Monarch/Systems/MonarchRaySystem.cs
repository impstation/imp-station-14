using Content.Server._Impstation.Monarch.Events;
using Content.Server._Impstation.Monarch.Components;
using Content.Shared._Impstation.Consume.Components;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Damage;
using Content.Shared.Players;
using Content.Shared.Mind;
using Content.Shared.Damage.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
namespace Content.Server._Impstation.Monarch;

public sealed class MonarchRaySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly DamageableSystem _damageSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MonarchRayComponent, ClothingGotEquippedEvent>(OnClothingEquip);
        SubscribeLocalEvent<MonarchRayComponent, ClothingGotUnequippedEvent>(OnClothingUnequip);
    }

    private void OnClothingEquip(Entity<MonarchRayComponent> ent, ref ClothingGotEquippedEvent args)
    {
        ent.Comp.CurrentWearer = args.Wearer;

        // grub steal mind
        OnMonarchStealMind(ent);

        // wearing the grub sets off this event. idk what to do with it yet but its here
        var ev = new MonarchStealMindEvent(ent);
        RaiseLocalEvent(ent, ref ev);
        // test
    }

    private void OnClothingUnequip(Entity<MonarchRayComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        ent.Comp.CurrentWearer = null;
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
            _mindSystem.TransferTo(mind, ent);
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