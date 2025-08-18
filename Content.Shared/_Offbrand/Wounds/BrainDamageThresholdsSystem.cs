using System.Linq;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.Wounds;

public sealed partial class BrainDamageThresholdsSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BrainDamageThresholdsComponent, AfterBrainDamageChanged>(OnAfterBrainDamageChanged);
        SubscribeLocalEvent<BrainDamageThresholdsComponent, AfterBrainOxygenChanged>(OnAfterBrainOxygenChanged);
        SubscribeLocalEvent<BrainDamageThresholdsComponent, UpdateMobStateEvent>(OnUpdateMobState);
    }

    private void UpdateState(Entity<BrainDamageThresholdsComponent> ent)
    {
        var brain = Comp<BrainDamageComponent>(ent);

        var damageState = ent.Comp.DamageStateThresholds.HighestMatch(brain.Damage) ?? MobState.Alive;
        var oxygenState = ent.Comp.OxygenStateThresholds.LowestMatch(brain.Oxygen) ?? MobState.Alive;

        var state = (byte)damageState > (byte)oxygenState ? damageState : oxygenState;

        if (state == ent.Comp.CurrentState)
            return;

        ent.Comp.CurrentState = state;
        Dirty(ent);
        _mobState.UpdateMobState(ent);
    }

    private void OnAfterBrainDamageChanged(Entity<BrainDamageThresholdsComponent> ent, ref AfterBrainDamageChanged args)
    {
        var brain = Comp<BrainDamageComponent>(ent);

        UpdateState(ent);

        var damageEffect = ent.Comp.DamageEffectThresholds.HighestMatch(brain.Damage);
        if (damageEffect == ent.Comp.CurrentDamageEffect)
            return;

        if (ent.Comp.CurrentDamageEffect is { } oldEffect)
            _statusEffects.TryRemoveStatusEffect(ent, oldEffect);

        if (damageEffect is { } newEffect)
            _statusEffects.TryUpdateStatusEffectDuration(ent, newEffect, out _);

        ent.Comp.CurrentDamageEffect = damageEffect;
        Dirty(ent);

        var overlays = new PotentiallyUpdateDamageOverlay(ent);
        RaiseLocalEvent(ent, ref overlays, true);
    }

    private void OnAfterBrainOxygenChanged(Entity<BrainDamageThresholdsComponent> ent, ref AfterBrainOxygenChanged args)
    {
        var brain = Comp<BrainDamageComponent>(ent);

        UpdateState(ent);

        var oxygenEffect = ent.Comp.OxygenEffectThresholds.LowestMatch(brain.Oxygen);
        if (oxygenEffect == ent.Comp.CurrentOxygenEffect)
            return;

        if (ent.Comp.CurrentOxygenEffect is { } oldEffect)
            _statusEffects.TryRemoveStatusEffect(ent, oldEffect);

        if (oxygenEffect is { } newEffect)
            _statusEffects.TryUpdateStatusEffectDuration(ent, newEffect, out _);

        ent.Comp.CurrentOxygenEffect = oxygenEffect;
        Dirty(ent);

        var overlays = new PotentiallyUpdateDamageOverlay(ent);
        RaiseLocalEvent(ent, ref overlays, true);
    }

    private void OnUpdateMobState(Entity<BrainDamageThresholdsComponent> ent, ref UpdateMobStateEvent args)
    {
        args.State = ent.Comp.CurrentState;

        var overlays = new PotentiallyUpdateDamageOverlay(ent);
        RaiseLocalEvent(ent, ref overlays, true);
    }
}
