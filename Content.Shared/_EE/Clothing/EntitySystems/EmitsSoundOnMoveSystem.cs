// SPDX-FileCopyrightText: 2024 FoxxoTrystan <45297731+FoxxoTrystan@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 VMSolidus <evilexecutive@gmail.com>
// SPDX-FileCopyrightText: 2025 sleepyyapril <123355664+sleepyyapril@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using Content.Shared._EE.Clothing.Components;
using Content.Shared._EE.Movement.Events;
using Content.Shared.Clothing.Components;
using Content.Shared.Gravity;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._EE.Clothing.EntitySystems;

public sealed class EmitsSoundOnMoveSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private EntityQuery<ClothingComponent> _clothingQuery;

    public override void Initialize()
    {
        base.Initialize();
        _clothingQuery = GetEntityQuery<ClothingComponent>();
        SubscribeLocalEvent<EmitsSoundOnMoveComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<EmitsSoundOnMoveComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<EmitsSoundOnMoveComponent, InventoryRelayedEvent<MakeFootstepSoundEvent>>(OnFootstep);
    }

    private void OnEquipped(Entity<EmitsSoundOnMoveComponent> ent, ref GotEquippedEvent args)
    {
        ent.Comp.IsSlotValid = !args.SlotFlags.HasFlag(ent.Comp.InvalidSlots);
    }

    private void OnUnequipped(Entity<EmitsSoundOnMoveComponent> ent, ref GotUnequippedEvent args)
    {
        ent.Comp.IsSlotValid = true;
    }

    private void OnFootstep(Entity<EmitsSoundOnMoveComponent> ent, ref InventoryRelayedEvent<MakeFootstepSoundEvent> args)
    {
        var uid = ent.Owner;
        var component = ent.Comp;

        if (!_timing.IsFirstTimePredicted)
            return;
        if (_timing.CurTime < ent.Comp.CooldownTimer)
            return;
        var xform = Transform(uid);
        // Space does not transmit sound
        if (xform.GridUid is null)
            return;

        var parent = xform.ParentUid;

        var isWorn = parent is { Valid: true } &&
                     _clothingQuery.TryGetComponent(uid, out var clothing)
                     && clothing.InSlot != null
                     && component.IsSlotValid;

        if (component.RequiresWorn && !isWorn)
            return;

        var sound = component.SoundCollection;
        sound.Params = sound.Params
                        .WithVolume(sound.Params.Volume)
                        .WithVariation(sound.Params.Variation ?? 0f);

        _audio.PlayPredicted(sound, uid, uid);
        ent.Comp.CooldownTimer=_timing.CurTime + ent.Comp.SoundCooldown;
        Dirty(ent);
    }
}
