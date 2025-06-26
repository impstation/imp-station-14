using Content.Shared.Gravity;
using Content.Shared.EntityEffects;
using Content.Shared.Movement.Systems;
using Content.Shared.Movement.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
///     Makes a mob Unweightful.
/// </summary>
public sealed partial class AntiGravity : EntityEffect
{

    public override void Effect(EntityEffectBaseArgs args)
    {
        var weightless = args.EntityManager.EnsureComponent<MovementIgnoreGravityComponent>(args.TargetEntity);
        weightless.Weightless = true;
        args.EntityManager.Dirty(args.TargetEntity, weightless);
    }

      [DataField("gravityState")] public bool Weightless = true;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Grants whoever drinks this the ability to see ghosts for a while";
    }
}
