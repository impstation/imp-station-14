using Content.Server.Objectives.Components;
using Content.Server.Roles;
using Content.Server._Impstation.Tourist.Components;
using Content.Shared.Objectives.Components;
using Content.Shared._Impstation.Tourist.Components;
using Content.Shared.Roles;
using Content.Shared.Warps;
using Robust.Shared.Random;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles tourist objective conditions
/// </summary>
public sealed class TouristConditionsSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly NumberObjectiveSystem _number = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TouristPhotosConditionComponent, ObjectiveGetProgressEvent>(OnPhotosGetProgress);
    }

    // multiple photographs of something

    private void OnPhotosGetProgress(EntityUid uid, TouristPhotosConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = PhotoProgress(comp, _number.GetTarget(uid));
    }

    private float PhotoProgress(TouristPhotosConditionComponent comp, int target)
    {
        // prevent divide-by-zero
        if (target == 0)
            return 1f;

        return MathF.Min(comp.Photos / (float)target, 1f);
    }
}
