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
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Impstation.Notifier;


public abstract partial class SharedNotifierSystem : EntitySystem
{
    [Dependency] protected readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] protected readonly ILogManager _log = default!;
    [Dependency] protected readonly IEntityManager _entManager = default!;
    protected ISawmill _sawmill = default!;


    private readonly ResPath _accessibilityIcon = new("/Textures/_Impstation/Interface/VerbIcons/star.svg.192dpi.png");


    public override void Initialize()
    {
        _sawmill = _log.GetSawmill("notifier");
        SubscribeLocalEvent<NotifierComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<NotifierComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<NotifierComponent, GetVerbsEvent<ExamineVerb>>(OnGetExamineVerbs);
    }

    public bool TryGetNotifier(NetUserId userId, [NotNullWhen(true)] out PlayerNotifierSettings? notifierSettings)
    {
        var entity = _mindSystem.GetOrCreateMind(userId).Comp.CurrentEntity;
        var exists = _entManager.TryGetComponent<NotifierComponent>(entity, out var notifierComponent);
        notifierSettings = notifierComponent?.Settings;
        return exists;
    }

    public virtual void SetPlayerNotifier(NetUserId userId, PlayerNotifierSettings? notifierSettings)
    {
        if (notifierSettings == null)
        {
            return;
        }
        var entity = _mindSystem.GetOrCreateMind(userId).Comp.CurrentEntity;
        var exists = _entManager.TryGetComponent<NotifierComponent>(entity, out var notifierComponent);
        if (exists)
        {
            notifierComponent!.Settings = notifierSettings;
            Dirty(entity!.Value,notifierComponent);
        }
    }

    private void OnGetExamineVerbs(Entity<NotifierComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
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

    private void OnExamined(Entity<NotifierComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.Settings.Enabled || !args.IsInDetailsRange || _mobState.IsDead(ent.Owner)) return;
        args.PushMarkup($"[color=lightblue]{Loc.GetString("notifier-info", ("ent", ent.Owner))}[/color]");
        Dirty(ent.Owner,ent.Comp);
    }

    protected virtual string GetNotifierText(NetUserId userId)
    {
        return "";
    }

    protected virtual bool GetNotifierEnabled(NetUserId userId)
    {
        return false;
    }

    private void OnPlayerAttached(Entity<NotifierComponent> ent, ref PlayerAttachedEvent args)
    {
        ent.Comp.AttachedUserId = args.Player.UserId;
        ent.Comp.Settings.Enabled=GetNotifierEnabled(args.Player.UserId);
        ent.Comp.Settings.Freetext=GetNotifierText(args.Player.UserId);
    }
}
