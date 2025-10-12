using Content.Shared._Impstation.StatusEffectNew;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Pidgin;
using Robust.Client.Player;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Impstation.StatusEffects;

public sealed class BiomagneticPolarizationSystem : SharedBiomagneticPolarizationSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;

    private readonly EntProtoId _effectID = "StatusEffectBiomagneticPolarization";

    public override void Update(float frameTime)
    {
        var maybePlayer = _player.LocalEntity;
        if (maybePlayer is not { } player)
            return;

        if (_statusEffect.TryGetStatusEffect(player, _effectID, out var effect)
            && TryComp<BiomagneticPolarizationStatusEffectComponent>(effect, out var biomagComp))
        {
            HandleCollisions(biomagComp.StatusOwner, biomagComp);
        }

        base.Update(frameTime);
    }
}
