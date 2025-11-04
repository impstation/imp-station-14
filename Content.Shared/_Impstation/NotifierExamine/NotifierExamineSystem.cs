using Content.Shared._Impstation.CCVar;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared._Impstation.NotifierExamine;

public sealed class NotifierExamineSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly INetConfigurationManager _netCfg = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    private readonly ResPath _accessibilityIcon = new("/Textures/_Impstation/Interface/VerbIcons/star.svg.192dpi.png");
    public override void Initialize()
    {

        SubscribeLocalEvent<NotifierExamineComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<NotifierExamineComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<NotifierExamineComponent, GetVerbsEvent<ExamineVerb>>(OnGetExamineVerbs);
    }

    private void OnPlayerAttached(Entity<NotifierExamineComponent> ent, ref PlayerAttachedEvent args)
    {
        if (!_netCfg.GetClientCVar(args.Player.Channel, ImpCCVars.NotifierOn))
            return;

        ent.Comp.Active = true;
        ent.Comp.Content = _netCfg.GetClientCVar(args.Player.Channel, ImpCCVars.NotifierExamine);
        Dirty(ent.Owner, ent.Comp);
    }
    private void OnGetExamineVerbs(Entity<NotifierExamineComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!ent.Comp.Active || Identity.Name(args.Target, EntityManager) != MetaData(args.Target).EntityName)
            return;

        var user = args.User;
        var verb = new ExamineVerb
        {
            Act = () =>
            {
                var markup = new FormattedMessage();
                markup.AddText(ent.Comp.Content);
                _examine.SendExamineTooltip(user, ent, markup, false, false);
            },
            Text = Loc.GetString("notifier-verb-text"),
            Category = VerbCategory.Examine,
            Icon = new SpriteSpecifier.Texture(_accessibilityIcon)
        };
        Dirty(ent.Owner, ent.Comp);
        args.Verbs.Add(verb);
    }

    private void OnExamined(Entity<NotifierExamineComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.Active || !args.IsInDetailsRange || _mobState.IsDead(ent.Owner)) return;
        args.PushMarkup($"[color=lightblue]{Loc.GetString("notifier-info", ("ent", ent.Owner))}[/color]");
    }
}
