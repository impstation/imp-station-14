// SPDX-FileCopyrightText: 2025 BuildTools <unconfigured@null.spigotmc.org>
// SPDX-FileCopyrightText: 2025 mycobiota <154991750+mycobiota@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server._Funkystation.Tools.Components;
using Content.Shared.Item.ItemToggle.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;


namespace Content.Server._Funkystation.Tools.Systems;

public sealed class CheapLighterSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CheapLighterComponent, ItemToggleDeactivateAttemptEvent>(OnLighterClose);
        SubscribeLocalEvent<CheapLighterComponent, ItemToggleActivateAttemptEvent>(OnLighterOpen);
    }

    private void OnLighterClose(Entity<CheapLighterComponent> ent, ref ItemToggleDeactivateAttemptEvent args)
    {
        _audio.Stop(ent.Comp.LighterStream);
    }

    private void OnLighterOpen(Entity<CheapLighterComponent> ent, ref ItemToggleActivateAttemptEvent args)
    {
        if (_random.NextFloat() < ent.Comp.FailChance)
        {
            args.Cancelled = true;
            _audio.PlayPvs(ent.Comp.SoundFail, ent);
            return;
        }

        if (!args.Cancelled && ent.Comp.SoundActivate != null)
        {
            _audio.Stop(ent.Comp.LighterStream);
            ent.Comp.LighterStream= _audio.PlayPvs(ent.Comp.SoundActivate, ent)?.Entity;
        }
    }
}
