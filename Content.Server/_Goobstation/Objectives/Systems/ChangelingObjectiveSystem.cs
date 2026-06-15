using Content.Server._Goobstation.Objectives.Components;
using Content.Server.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Shared.Objectives.Components;

namespace Content.Server._Goobstation.Objectives.Systems;

public sealed partial class ChangelingObjectiveSystem : EntitySystem
{
    [Dependency] private readonly NumberObjectiveSystem _number = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GoobAbsorbConditionComponent, ObjectiveGetProgressEvent>(OnAbsorbGetProgress);
        SubscribeLocalEvent<GoobStealDNAConditionComponent, ObjectiveGetProgressEvent>(OnStealDNAGetProgress);
    }

    private void OnAbsorbGetProgress(EntityUid uid, GoobAbsorbConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        var target = _number.GetTarget(uid);
        if (target != 0)
            args.Progress = MathF.Min(comp.Absorbed / target, 1f);
        else args.Progress = 1f;
    }
    private void OnStealDNAGetProgress(EntityUid uid, GoobStealDNAConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        var target = _number.GetTarget(uid);
        if (target != 0)
            args.Progress = MathF.Min(comp.DNAStolen / target, 1f);
        else args.Progress = 1f;
    }
}
