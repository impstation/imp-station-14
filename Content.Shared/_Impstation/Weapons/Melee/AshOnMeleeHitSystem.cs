using Content.Shared._EE.Supermatter.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Humanoid;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._Impstation.Weapons.Melee;

public sealed class AshOnMeleeHitSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly ISharedChatManager _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AshOnMeleeHitComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<AshOnMeleeHitComponent, ThrowDoHitEvent>(OnThrowHit);
    }

    private void OnMeleeHit(Entity<AshOnMeleeHitComponent> ent, ref MeleeHitEvent args)
    {
        if (args.Handled || args.HitEntities.Count < 1)
            return;

        var anyAshed = false;

        foreach (var target in args.HitEntities)
        {
            Ash(ent, target);
            anyAshed = true;
        }

        if (anyAshed == false)
            return;

        _audio.PlayPvs(ent.Comp.Sound, Transform(ent).Coordinates);

        if (ent.Comp.SingleUse)
            EntityManager.QueueDeleteEntity(ent);
    }

    private void OnThrowHit(Entity<AshOnMeleeHitComponent> ent, ref ThrowDoHitEvent args)
    {
        if (HasComp<SupermatterImmuneComponent>(args.Target))
            return;

        Ash(ent, args.Target);
        _audio.PlayPvs(ent.Comp.Sound, Transform(ent).Coordinates);

        if (ent.Comp.SingleUse)
            EntityManager.QueueDeleteEntity(ent);
    }

    private void Ash(Entity<AshOnMeleeHitComponent> ent, EntityUid target)
    {
        var coords = Transform(target).Coordinates;
        var isHumanoid = HasComp<HumanoidAppearanceComponent>(target);
        var logImpact = isHumanoid ? LogImpact.High : LogImpact.Medium;

        _popup.PopupCoordinates(Loc.GetString(ent.Comp.Popup, ("entity", ent.Owner), ("target", target)), coords, PopupType.LargeCaution);
        _adminLog.Add(LogType.Damaged, logImpact, $"{EntityManager.ToPrettyString(target):target} was ashed by {EntityManager.ToPrettyString(ent.Owner):uid}");

        if (isHumanoid)
            _chat.SendAdminAlert($"{EntityManager.ToPrettyString(target):target} was ashed by {EntityManager.ToPrettyString(ent.Owner):uid}");

        EntityManager.SpawnEntity(ent.Comp.AshPrototype, coords);
        EntityManager.QueueDeleteEntity(target);
    }
}
