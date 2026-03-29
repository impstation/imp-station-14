using System.Diagnostics.CodeAnalysis;
using Content.Shared._Impstation.CCVar;
using Content.Shared.Cloning.Events;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Roles.Components;
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
        SubscribeLocalEvent<NotifierComponent, CloningEvent>(OnClone);
    }

    /// <summary>
    /// Is triggered on cloning an entity with the notifier component.
    /// Clones the originals notifier to the clone.
    /// If this is a paradox clone, create a copycat.
    /// </summary>
    private void OnClone(Entity<NotifierComponent> ent, ref CloningEvent args)
    {

        if (args.Settings.ID.Equals("ParadoxCloningSettings"))
        {
            CreateCopyCat(ent , args.CloneUid);
            return;
        }
        var cloneNotifier = EnsureComp<NotifierComponent>(args.CloneUid);

        cloneNotifier.Settings = ent.Comp.Settings;
        cloneNotifier.AttachedUserId = ent.Comp.AttachedUserId;

    }

    /// <summary>
    /// Get The notifier from the target players current entity.
    /// </summary>
    /// <param name="userId">Target user</param>
    /// <param name="notifierSettings"> The users notifier</param>
    /// <returns></returns>
    public bool TryGetNotifier(NetUserId userId, [NotNullWhen(true)] out PlayerNotifierSettings? notifierSettings)
    {
        var entity = _mindSystem.GetOrCreateMind(userId).Comp.CurrentEntity;
        var exists = _entManager.TryGetComponent<NotifierComponent>(entity, out var notifierComponent);
        notifierSettings = notifierComponent?.Settings;
        return exists;
    }

    /// <summary>
    /// Set the players current entity notifier settings to the provided settings.
    /// If there are any copycats, pass the new notifier settings to them.
    /// </summary>
    /// <param name="userId">Target player</param>
    /// <param name="notifierSettings">New notifier settings</param>
    public virtual void SetPlayerNotifier(NetUserId userId, PlayerNotifierSettings? notifierSettings)
    {
        if (notifierSettings == null)
        {
            return;
        }
        var entity = _mindSystem.GetOrCreateMind(userId).Comp.CurrentEntity;
        var exists = _entManager.TryGetComponent<NotifierComponent>(entity, out var notifierComponent);
        if (!exists)
            return;
        notifierComponent!.Settings = notifierSettings;
        Dirty(entity!.Value,notifierComponent);
        foreach (var copycat in notifierComponent.Copycats)
        {
            var copycatEntity = GetEntity(copycat);
            EnsureComp<NotifierCopycatComponent>(copycatEntity, out var notifierCopycat);
            notifierCopycat.Settings = notifierSettings;
            Dirty(copycatEntity,notifierCopycat);
        }
    }
    /// <summary>
    ///  Loads the notifier settings attached to the entity into the examine text
    /// </summary>
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

    /// <summary>
    /// Adds the notifier examine verb as well as the disclaimer to the examine text
    /// </summary>
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

    /// <summary>
    ///  Gets the players notifier setting and adds them to the entity the player is attaching to.
    /// </summary>
    private void OnPlayerAttached(Entity<NotifierComponent> ent, ref PlayerAttachedEvent args)
    {
        ent.Comp.AttachedUserId = args.Player.UserId;
        ent.Comp.Settings.Enabled=GetNotifierEnabled(args.Player.UserId);
        ent.Comp.Settings.Freetext=GetNotifierText(args.Player.UserId);
    }

    /// <summary>
    /// Add the NotifierCopycat component to the clone, and copies information from the original.
    /// </summary>
    /// <param name="original">Player being copied</param>
    /// <param name="clone">Clone being turned into a copycat</param>
    private void CreateCopyCat(Entity<NotifierComponent> original, EntityUid clone)
    {
        EnsureComp<NotifierCopycatComponent>(clone, out var component);
        component.OriginUserId = original.Comp.AttachedUserId;
        component.Settings = original.Comp.Settings;
        original.Comp.Copycats.Add(GetNetEntity(clone));
        Dirty(clone,component);
        Dirty(original);
    }
}
