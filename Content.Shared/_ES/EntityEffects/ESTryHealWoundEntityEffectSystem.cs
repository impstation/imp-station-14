using Content.Shared._Offbrand.Wounds;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.Localizations;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.EntityEffects;

/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class ESTryHealWoundEntityEffectSystem : EntityEffectSystem<WoundableComponent, ESTryHealWound>
{
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly WoundableSystem _wound = default!;

    protected override void Effect(Entity<WoundableComponent> entity, ref EntityEffectEvent<ESTryHealWound> args)
    {
        if (!_status.TryGetStatusEffect(entity, args.Effect.Wound, out var woundEntity))
            return;

        if (!TryComp<WoundComponent>(woundEntity, out var wound))
            return;

        var dmg = args.Effect.Damage * args.Scale;
        _wound.TryHealDamageOnWound(entity!, (woundEntity.Value, wound), dmg);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class ESTryHealWound : EntityEffectBase<ESTryHealWound>
{
    /// <summary>
    ///     Wound to try and target.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId<WoundComponent> Wound = default!;

    /// <summary>
    ///     Damage to try and heal on the wound--gets scaled.
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier Damage = default!;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("es-entity-effect-guidebook-try-heal-wound", ("chance", Probability), ("wound", prototype.Index(Wound).Name));
}
