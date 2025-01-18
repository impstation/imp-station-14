using Content.Server.Objectives.Components;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

public sealed partial class CosmicCultObjectiveSystem : EntitySystem
{
    // [Dependency] private readonly NumberObjectiveSystem _number = default!;

    // public override void Initialize()
    // {
    //     base.Initialize();

    //     SubscribeLocalEvent<CosmicEntropyConditionComponent, ObjectiveGetProgressEvent>(OnSiphonProgress);
    // }

    // private void OnSiphonProgress(EntityUid uid, CosmicEntropyConditionComponent comp, ref ObjectiveGetProgressEvent args)
    // {
    //     var target = _number.GetTarget(uid);
    //     if (target != 0)
    //         args.Progress = MathF.Min(comp.Siphoned / target, 1f);
    //     else args.Progress = 1f;
    // }
}
