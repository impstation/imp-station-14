using Content.Shared._Offbrand.Wounds;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.EntityEffects;

/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class ESAddWoundEntityEffectSystem : EntityEffectSystem<WoundableComponent, ESAddWound>
{
    [Dependency] private readonly WoundableSystem _wound = default!;

    protected override void Effect(Entity<WoundableComponent> entity, ref EntityEffectEvent<ESAddWound> args)
    {
        _wound.TryWound(entity, args.Effect.Wound, null, args.Effect.Unique, args.Effect.RefreshDamage);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class ESAddWound : EntityEffectBase<ESAddWound>
{
    /// <summary>
    ///     Wound to add.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId<WoundComponent> Wound = default!;

    /// <summary>
    ///     Should the wound be unique (i.e. no copies will be added if one already exists?)
    /// </summary>
    [DataField]
    public bool Unique = true;

    [DataField]
    public bool RefreshDamage = true;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("es-entity-effect-guidebook-add-wound", ("chance", Probability), ("wound", prototype.Index(Wound).Name));
}
