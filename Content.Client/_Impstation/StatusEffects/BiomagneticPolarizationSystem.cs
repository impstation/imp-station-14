using Content.Client.Administration.Systems;
using Content.Shared._Impstation.StatusEffectNew;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Pidgin;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Impstation.StatusEffects;

public sealed class BiomagneticPolarizationSystem : SharedBiomagneticPolarizationSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;

    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private readonly EntProtoId _effectID = "StatusEffectBiomagneticPolarization";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BiomagneticPolarizationStatusEffectComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<BiomagneticPolarizationStatusEffectComponent, ComponentShutdown>(OnShutdown);
    }

    public override void Update(float frameTime)
    {
        var maybePlayer = _player.LocalEntity;
        if (maybePlayer is not { } player)
            return;

        HandlePrediction(player);

        var query = EntityQueryEnumerator<BiomagneticPolarizationStatusEffectComponent>();
        while (query.MoveNext(out var ent, out var comp))
        {
            // skip all the sprite shit if capped status hasn't changed since last frame, so we're only doing it once.
            if (comp.Capped == comp.LastCapped)
                continue;

            if (comp.Capped)
                AddCappedSprite((ent, comp));
            else
                RemoveCappedSprite((ent, comp));
        }

        base.Update(frameTime);
    }

    public void OnInit(Entity<BiomagneticPolarizationStatusEffectComponent> ent, ref ComponentInit args)
    {
        if (!TryComp<StatusEffectComponent>(ent, out var statusEffect))
            return;

        if (!TryComp<PhysicsComponent>(statusEffect.AppliedTo, out var physComp) || statusEffect.AppliedTo is not { } appliedTo)
            return;

        Entity<PhysicsComponent?>? entPhys = (appliedTo, physComp);

        ent.Comp.StatusOwner = entPhys;
    }

    public void OnShutdown(Entity<BiomagneticPolarizationStatusEffectComponent> ent, ref ComponentShutdown args)
    {
        RemoveCappedSprite(ent);
    }

    public void HandlePrediction(EntityUid player)
    {
        if (_statusEffect.TryGetStatusEffect(player, _effectID, out var effect)
            && TryComp<BiomagneticPolarizationStatusEffectComponent>(effect, out var biomagComp))
        {
            HandleCollisions(biomagComp.StatusOwner, biomagComp);
        }
    }

    public void AddCappedSprite(Entity<BiomagneticPolarizationStatusEffectComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;
        _sprite.LayerSetVisible((ent, sprite), BiomagneticPolarizationLayers.Capped, true);
    }

    public void RemoveCappedSprite(Entity<BiomagneticPolarizationStatusEffectComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;
        _sprite.LayerSetVisible((ent, sprite), BiomagneticPolarizationLayers.Capped, false);
    }
}
