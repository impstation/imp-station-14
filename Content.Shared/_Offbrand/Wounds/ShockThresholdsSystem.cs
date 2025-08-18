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
    }

    private void OnAfterShockChange(Entity<ShockThresholdsComponent> ent, ref AfterShockChangeEvent args)
    {
        var shock = _pain.GetShock(ent.Owner);
        var targetEffect = ent.Comp.Thresholds.HighestMatch(shock);
        if (targetEffect == ent.Comp.CurrentThresholdState)
            return;

        var seenTarget = targetEffect is null;
        ent.Comp.CurrentThresholdState = targetEffect;
        Dirty(ent);
        foreach (var (threshold, effect) in ent.Comp.Thresholds)
        {
            if (!seenTarget)
            {
                seenTarget = effect == targetEffect;
                if (!_statusEffects.HasStatusEffect(ent, effect))
                    _statusEffects.TryUpdateStatusEffectDuration(ent, effect, out _);
            }
            else
            {
                _statusEffects.TryRemoveStatusEffect(ent, effect);
            }
        }
    }

    public bool IsCritical(Entity<ShockThresholdsComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        return ent.Comp.CurrentThresholdState == ent.Comp.Thresholds.Last().Value;
    }
}
