using Content.Server._Impstation.StrangeMoods;
using Content.Shared._Impstation.EntityEffects.Effects.Thaven;
using Content.Shared.EntityEffects;
using Content.Shared._Impstation.StrangeMoods;

namespace Content.Server._Impstation.EntityEffects.Effects.Thaven;

public sealed class ThavenRefreshMoodsEffectSystem : EntityEffectSystem<StrangeMoodsComponent, ThavenRefreshMoods>
{
    [Dependency] private readonly StrangeMoodsSystem _moods = default!;

    protected override void Effect(Entity<StrangeMoodsComponent> ent, ref EntityEffectEvent<ThavenRefreshMoods> args)
    {
        _moods.RefreshMoods(ent);
    }
}
