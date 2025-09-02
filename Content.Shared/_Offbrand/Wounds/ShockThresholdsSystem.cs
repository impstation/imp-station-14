/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Linq;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.Wounds;

public sealed partial class ShockThresholdsSystem : EntitySystem
{
    [Dependency] private readonly PainSystem _pain = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShockThresholdsComponent, AfterShockChangeEvent>(OnAfterShockChange);
        SubscribeLocalEvent<ShockThresholdsComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<ShockThresholdsComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.CurrentThresholdState is { } effect)
            _statusEffects.TryRemoveStatusEffect(ent, effect);
    }

    private void OnAfterShockChange(Entity<ShockThresholdsComponent> ent, ref AfterShockChangeEvent args)
    {
        var shock = _pain.GetShock(ent.Owner);
        var targetEffect = ent.Comp.Thresholds.HighestMatch(shock);
        if (targetEffect == ent.Comp.CurrentThresholdState)
            return;

        var seenTarget = targetEffect is null;
        if (ent.Comp.CurrentThresholdState is { } oldEffect)
            _statusEffects.TryRemoveStatusEffect(ent, oldEffect);

        if (targetEffect is { } effect)
            _statusEffects.TryUpdateStatusEffectDuration(ent, effect, out _);

        ent.Comp.CurrentThresholdState = targetEffect;
        Dirty(ent);
    }

    public bool IsCritical(Entity<ShockThresholdsComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        return ent.Comp.CurrentThresholdState == ent.Comp.Thresholds.Last().Value;
    }
}
