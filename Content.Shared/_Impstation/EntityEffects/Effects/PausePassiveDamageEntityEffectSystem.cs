using Content.Shared.Damage.Components;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._Impstation.EntityEffects.Effects;

public sealed class PausePassiveDamageEntityEffectSystem : EntityEffectSystem<MetaDataComponent, PausePassiveDamage>
{
    [Dependency] private readonly IGameTiming _timing = default!;
    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<PausePassiveDamage> args)
    {
        if (!TryComp<PassiveDamageComponent>(entity, out var damage))
        {
            return;
        }

        damage.DamagePause= _timing.CurTime + args.Effect.PauseTime;
    }
}

public sealed partial class PausePassiveDamage: EntityEffectBase<PausePassiveDamage>
{
    [DataField("pauseTime")]
    public TimeSpan PauseTime = TimeSpan.Zero;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-pause-passive-damage", ("time", PauseTime.Seconds));
}
