using Content.Shared._Impstation.CCVar;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
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
    public override void Initialize()
    {
        SubscribeLocalEvent<NotifierExamineComponent, PlayerAttachedEvent>(OnPlayerAttached);
        //SubscribeLocalEvent<NotifierExamineComponent, PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<NotifierExamineComponent, GetVerbsEvent<ExamineVerb>>(OnGetExamineVerbs);
    }

    private void OnPlayerAttached(EntityUid uid,NotifierExamineComponent component, PlayerAttachedEvent args)
    {
        var add=_netCfg.GetClientCVar<bool>(args.Player.Channel, ImpCCVars.ShowNotifierExamine);
        if (add)
        {
            component.Active=true;
            var content=_netCfg.GetClientCVar<string>(args.Player.Channel, ImpCCVars.NotifierExamine);
            component.Content = content;
        }
        Dirty(uid,component);
    }
    private void OnGetExamineVerbs(Entity<NotifierExamineComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if ((Identity.Name(args.Target, EntityManager) != MetaData(args.Target).EntityName)||!ent.Comp.Active)
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
}
