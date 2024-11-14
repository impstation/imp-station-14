using Content.Server.Objectives.Components;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

public sealed class CosmicCultRecruitingConditionSystem : EntitySystem
{
    [Dependency] private readonly NumberObjectiveSystem _number = default!;

    // public override void Initialize()
    // {
    //     base.Initialize();

    //     SubscribeLocalEvent<CosmicEntropyConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    // }

    // private void OnGetProgress(Entity<CosmicEntropyConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    // {
    //     args.Progress = Progress(ent.Comp.Siphoned, _number.GetTarget(ent.Owner));
    // }

    // private float Progress(int recruited, int target)
    // {
    //     // prevent divide-by-zero
    //     if (target == 0)
    //         return 1f;

    //     return MathF.Min(recruited / (float) target, 1f);
    // }
}
