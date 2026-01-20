using System.Diagnostics.CodeAnalysis;
using Content.Shared._Impstation.CCVar;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared._Impstation.Notifier;

public abstract partial class SharedNotifierSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly INetConfigurationManager _netCfg = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly ILogManager _log = default!;
    private ISawmill _sawmill = default!;

    protected readonly Dictionary<NetUserId, PlayerNotifierSettings> UserNotifiers = new();
    private readonly ResPath _accessibilityIcon = new("/Textures/_Impstation/Interface/VerbIcons/star.svg.192dpi.png");
    public override void Initialize()
    {
        //_sawmill = _log.GetSawmill("consent");
        SubscribeLocalEvent<NotifierComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<NotifierComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<NotifierComponent, GetVerbsEvent<ExamineVerb>>(OnGetExamineVerbs);
    }

    public bool TryGetNotifier(NetUserId userId, [NotNullWhen(true)] out PlayerNotifierSettings? notifierSettings)
    {
        var exists = UserNotifiers.TryGetValue(userId, out notifierSettings);
        return exists;
    }

    public virtual void SetNotifier(NetUserId userId, PlayerNotifierSettings? notifierSettings)
    {
        if (notifierSettings == null)
        {
            UserNotifiers.Remove(userId);
            return;
        }

        UserNotifiers[userId] = notifierSettings;
    }

    private void OnPlayerAttached(Entity<NotifierComponent> ent, ref PlayerAttachedEvent args)
    {

        var userId = args.Player.UserId;
        ent.Comp.Active = GetNotifierEnabled(userId);
        ent.Comp.Content = GetNotifierText(userId);

    }
    private void OnGetExamineVerbs(Entity<NotifierComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!ent.Comp.Active || Identity.Name(args.Target, EntityManager) != MetaData(args.Target).EntityName)
            return;

        var user = args.User;
        var verb = new ExamineVerb
        {
            Act = () =>
            {
                _examine.SendExamineTooltip(user, ent, ent.Comp.Content, false, false);
            },
            Text = Loc.GetString("notifier-verb-text"),
            Category = VerbCategory.Examine,
            Icon = new SpriteSpecifier.Texture(_accessibilityIcon)
        };
        Dirty(ent.Owner, ent.Comp);
        args.Verbs.Add(verb);
    }

    private void OnExamined(Entity<NotifierComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.Active || !args.IsInDetailsRange || _mobState.IsDead(ent.Owner)) return;
        args.PushMarkup($"[color=lightblue]{Loc.GetString("notifier-info", ("ent", ent.Owner))}[/color]");
    }

    protected virtual FormattedMessage GetNotifierText(NetUserId userId)
    {
        return new();
    }

    protected virtual bool GetNotifierEnabled(NetUserId userId)
    {
        return false;
    }
}
