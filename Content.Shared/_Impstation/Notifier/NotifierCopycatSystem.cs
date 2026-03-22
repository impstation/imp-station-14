using Content.Shared._Impstation.Notifier.events;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared._Impstation.Notifier;

public sealed class NotifierCopycatSystem : EntitySystem
{
    private readonly ResPath _accessibilityIcon = new("/Textures/_Impstation/Interface/VerbIcons/star.svg.192dpi.png");
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NotifierCopycatComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<NotifierCopycatComponent, GetVerbsEvent<ExamineVerb>>(OnGetExamineVerbs);
    }

    private void OnGetExamineVerbs(Entity<NotifierCopycatComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!ent.Comp.Settings.Enabled || Identity.Name(args.Target, EntityManager) != MetaData(args.Target).EntityName)
            return;

        var user = args.User;
        var verb = new ExamineVerb
        {
            Act = () =>
            {
                var message = new FormattedMessage();
                message.AddText(ent.Comp.Settings.Freetext);
                _examine.SendExamineTooltip(user, ent, message, false, false);
            },
            Text = Loc.GetString("notifier-verb-text"),
            Category = VerbCategory.Examine,
            Icon = new SpriteSpecifier.Texture(_accessibilityIcon)
        };
        args.Verbs.Add(verb);
        Dirty(ent.Owner,ent.Comp);
    }

    private void OnExamined(Entity<NotifierCopycatComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.Settings.Enabled || !args.IsInDetailsRange || _mobState.IsDead(ent.Owner)) return;
        args.PushMarkup($"[color=lightblue]{Loc.GetString("notifier-info", ("ent", ent.Owner))}[/color]");
        Dirty(ent.Owner,ent.Comp);
    }
}


