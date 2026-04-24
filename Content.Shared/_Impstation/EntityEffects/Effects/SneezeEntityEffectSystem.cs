using Content.Shared.Medical;
using Content.shared._Impstation.Medical;
using Robust.Shared.Prototypes;
using Content.Shared.Chemistry.Reagent; // imp add

namespace Content.Shared._Impstation.EntityEffects.Effects.Body;

/// <summary>
/// Makes an entity sneeze.
/// </summary>
public sealed partial class SneezeEntityEffectSystem : EntityEffectSystem<MetaDataComponent, Sneeze>
{
    [Dependency] private readonly SneezeSystem _sneeze = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<Sneeze> args)
    {
        _sneeze.Sneeze(entity.Owner, overridePrototype: args.Effect.MucusPrototype);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class Sneeze : EntityEffectBase<Sneeze>
{
    [DataField]
    public ProtoId<ReagentPrototype>? MucusPrototype;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        if (prototype.Resolve(MucusPrototype, out var mucusProto))
            return Loc.GetString("entity-effect-guidebook-sneeze-override",
                ("chance", Probability),
                ("override", mucusProto.LocalizedName));

        return Loc.GetString("entity-effect-guidebook-sneeze", ("chance", Probability));
    }
}
