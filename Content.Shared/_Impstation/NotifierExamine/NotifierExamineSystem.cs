using Content.Shared._Impstation.CCVar;
using Content.Shared._Impstation.Pleebnar.Components;
using Content.Shared.Actions;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Content.Shared.Players;
using Content.Shared.Verbs;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared._Impstation.NotifierExamine;

public sealed class NotifierExamineSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly INetConfigurationManager _netCfg = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    public override void Initialize()
    {

        SubscribeLocalEvent<NotifierExamineComponent,ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<NotifierExamineComponent, PlayerAttachedEvent>(OnPlayerAttached);
        //SubscribeLocalEvent<NotifierExamineComponent, PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<NotifierExamineComponent, GetVerbsEvent<ExamineVerb>>(OnGetExamineVerbs);


    }

    private void OnPlayerAttached(EntityUid uid,NotifierExamineComponent component, PlayerAttachedEvent args)
    {

        if ( _netCfg.GetClientCVar<bool>(args.Player.Channel, ImpCCVars.NotifierOn))
        {
            component.Active=true;
            component.IconOn = !_netCfg.GetClientCVar<bool>(args.Player.Channel, ImpCCVars.NotifierIconOffByDefault);
            component.Content=_netCfg.GetClientCVar<string>(args.Player.Channel, ImpCCVars.NotifierExamine);
            _actionsSystem.AddAction(uid,ref component.notifierIconToggle,component.notifierIconToggleActionId);
            _actionsSystem.AddAction(uid,ref component.notifierToggle,component.notifierToggleActionId);
        }

        Dirty(uid,component);
    }
    private void OnGetExamineVerbs(Entity<NotifierExamineComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!ent.Comp.Active)
            return;
        if (Identity.Name(args.Target, EntityManager) != MetaData(args.Target).EntityName)
            return;

        var user = args.User;
        var verb = new ExamineVerb
        {
            Act = () =>
            {
                var markup = new FormattedMessage();
                markup.AddMarkupPermissive(ent.Comp.Content);
                _examine.SendExamineTooltip(user, ent, markup, false, false);
            },
            Text = Loc.GetString("detail-examinable-verb-text"),
            Category = VerbCategory.Examine,
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/star.svg.192dpi.png"))
        };

        args.Verbs.Add(verb);
    }

    private void OnExamined(Entity<NotifierExamineComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.Active || !args.IsInDetailsRange||_mobState.IsDead(ent.Owner)) return;
        args.PushMarkup($"[color=lightblue]{Loc.GetString("notifier-info",("ent", ent.Owner))}[/color]");
    }
}
