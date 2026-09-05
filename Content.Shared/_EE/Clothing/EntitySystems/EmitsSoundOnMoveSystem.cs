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
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EmitsSoundOnMoveComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<EmitsSoundOnMoveComponent, InventoryRelayedEvent<MakeFootstepSoundEvent>>(OnFootstep);
    }

    //when equipped check if valid
    private void OnEquipped(Entity<EmitsSoundOnMoveComponent> ent, ref GotEquippedEvent args)
    {
        ent.Comp.IsSlotValid = !args.SlotFlags.HasFlag(ent.Comp.InvalidSlots);
    }

    /// <summary>
    /// Handle making sounds on footsteps
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="args"></param>
    private void OnFootstep(Entity<EmitsSoundOnMoveComponent> ent, ref InventoryRelayedEvent<MakeFootstepSoundEvent> args)
    {
        var uid = ent.Owner;
        var component = ent.Comp;

        if (!_timing.IsFirstTimePredicted)
            return;
        if(!ent.Comp.IsSlotValid)
            return;
        if (_timing.CurTime < ent.Comp.CooldownTimer)
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
