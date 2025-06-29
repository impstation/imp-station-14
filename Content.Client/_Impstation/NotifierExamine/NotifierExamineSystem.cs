using Content.Shared._Impstation.NotifierExamine;
using Content.Shared.CCVar;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Client._Impstation.NotifierExamine;

public sealed class NotifierExamineSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NotifierExamineComponent, GetStatusIconsEvent>(OnGetStatusIcon);
    }

    private void OnGetStatusIcon(EntityUid uid,  NotifierExamineComponent component, ref GetStatusIconsEvent args)
    {
        if (component.Active &&
            !_mobState.IsDead(uid) &&
            !HasComp<ActiveNPCComponent>(uid) &&
            TryComp<MindContainerComponent>(uid, out var mindContainer) &&
            mindContainer.ShowExamineInfo)
        {
            args.StatusIcons.Add(_prototype.Index(component.Icon));
        }
    }
}
