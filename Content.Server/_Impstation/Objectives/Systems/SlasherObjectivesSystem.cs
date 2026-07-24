using Content.Server._Impstation.GameTicking.Rules;
using Content.Server._Impstation.Objectives.Components;
using Content.Shared.Objectives.Components;
using Robust.Shared.GameObjects;

namespace Content.Server._Impstation.Objectives.Systems;

/// <summary>
/// Tracks progress for fixed Slasher objectives:
/// meathooks placed, effigy placed, and soul fragments fed.
/// </summary>
public sealed class SlasherObjectivesSystem : EntitySystem
{
    [Dependency] private readonly SlasherRuleSystem _rule = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    /// <summary>
    /// Subscribes local events and prepares dependencies for this system.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlasherMeathookConditionComponent, ObjectiveGetProgressEvent>(OnMeathookProgress);
        SubscribeLocalEvent<SlasherEffigyConditionComponent, ObjectiveGetProgressEvent>(OnEffigyProgress);
        SubscribeLocalEvent<SlasherFeedEffigyConditionComponent, ObjectiveGetProgressEvent>(OnFeedProgress);
        SubscribeLocalEvent<SlasherMeathookConditionComponent, ObjectiveAfterAssignEvent>(OnMeathookAfterAssign);
        SubscribeLocalEvent<SlasherEffigyConditionComponent, ObjectiveAfterAssignEvent>(OnEffigyAfterAssign);
        SubscribeLocalEvent<SlasherFeedEffigyConditionComponent, ObjectiveAfterAssignEvent>(OnFeedAfterAssign);
        SubscribeLocalEvent<SlasherDoNotKillFlavorConditionComponent, ObjectiveAfterAssignEvent>(OnDoNotKillAfterAssign);
    }

    /// <summary>
    /// Sets the localized name and description for the meathook objective after assignment.
    /// </summary>
    /// <param name="uid">Assigned objective entity.</param>
    /// <param name="comp">Objective condition component.</param>
    /// <param name="args">Objective assignment metadata.</param>
    private void OnMeathookAfterAssign(EntityUid uid, SlasherMeathookConditionComponent comp, ref ObjectiveAfterAssignEvent args)
    {
        _metaData.SetEntityName(uid, Loc.GetString("slasher-objective-condition-place-meathooks-title"), args.Meta);
        _metaData.SetEntityDescription(uid, Loc.GetString("slasher-objective-condition-place-meathooks-description"), args.Meta);
    }

    /// <summary>
    /// Sets the localized name and description for the effigy placement objective after assignment.
    /// </summary>
    /// <param name="uid">Assigned objective entity.</param>
    /// <param name="comp">Objective condition component.</param>
    /// <param name="args">Objective assignment metadata.</param>
    private void OnEffigyAfterAssign(EntityUid uid, SlasherEffigyConditionComponent comp, ref ObjectiveAfterAssignEvent args)
    {
        _metaData.SetEntityName(uid, Loc.GetString("slasher-objective-condition-place-effigy-title"), args.Meta);
        _metaData.SetEntityDescription(uid, Loc.GetString("slasher-objective-condition-place-effigy-description"), args.Meta);
    }

    /// <summary>
    /// Sets the localized name and description for the fragment-feeding objective after assignment.
    /// </summary>
    /// <param name="uid">Assigned objective entity.</param>
    /// <param name="comp">Objective condition component.</param>
    /// <param name="args">Objective assignment metadata.</param>
    private void OnFeedAfterAssign(EntityUid uid, SlasherFeedEffigyConditionComponent comp, ref ObjectiveAfterAssignEvent args)
    {
        _metaData.SetEntityName(uid, Loc.GetString("slasher-objective-condition-feed-fragments-title"), args.Meta);
        _metaData.SetEntityDescription(uid, Loc.GetString("slasher-objective-condition-feed-fragments-description"), args.Meta);
    }

    /// <summary>
    /// Sets the localized name and description for the flavor-only do-not-kill objective after assignment.
    /// </summary>
    /// <param name="uid">Assigned objective entity.</param>
    /// <param name="comp">Objective condition component.</param>
    /// <param name="args">Objective assignment metadata.</param>
    private void OnDoNotKillAfterAssign(EntityUid uid, SlasherDoNotKillFlavorConditionComponent comp, ref ObjectiveAfterAssignEvent args)
    {
        _metaData.SetEntityName(uid, Loc.GetString("slasher-objective-condition-do-not-kill-title"), args.Meta);
        _metaData.SetEntityDescription(uid, Loc.GetString("slasher-objective-condition-do-not-kill-description"), args.Meta);
    }

    // Progress = meathooks placed / required meathooks.
    /// <summary>
    /// Reports completion progress for the meathook objective.
    /// </summary>
    /// <param name="uid">Objective entity.</param>
    /// <param name="comp">Objective condition component.</param>
    /// <param name="args">Progress aggregation event.</param>
    private void OnMeathookProgress(EntityUid uid, SlasherMeathookConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (!_rule.TryGetActiveRule(out var rule))
        {
            args.Progress = 0f;
            return;
        }

        args.Progress = comp.Required <= 0
            ? 1f
            : MathF.Min(rule.Comp.MeathookCount / (float)comp.Required, 1f);
    }

    // Progress = 1.0 once the effigy has ever been placed.
    /// <summary>
    /// Reports completion progress for the effigy placement objective.
    /// </summary>
    /// <param name="uid">Objective entity.</param>
    /// <param name="comp">Objective condition component.</param>
    /// <param name="args">Progress aggregation event.</param>
    private void OnEffigyProgress(EntityUid uid, SlasherEffigyConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (!_rule.TryGetActiveRule(out var rule))
        {
            args.Progress = 0f;
            return;
        }

        args.Progress = rule.Comp.EffigyPlacedEver ? 1f : 0f;
    }

    // Progress = fragment insertions / target insertions.
    /// <summary>
    /// Reports completion progress for the fragment-feeding objective.
    /// </summary>
    /// <param name="uid">Objective entity.</param>
    /// <param name="comp">Objective condition component.</param>
    /// <param name="args">Progress aggregation event.</param>
    private void OnFeedProgress(EntityUid uid, SlasherFeedEffigyConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (!_rule.TryGetActiveRule(out var rule))
        {
            args.Progress = 0f;
            return;
        }

        var target = rule.Comp.TargetInsertions;
        args.Progress = target <= 0
            ? 1f
            : MathF.Min(rule.Comp.FragmentInsertions / (float)target, 1f);
    }
}
