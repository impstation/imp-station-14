using System.Linq;
using System.Text;
using Content.Server.Administration.Logs;
using Content.Server.Heretic.Components;
using Content.Server.Heretic.Ritual;
using Content.Shared._Goobstation.Heretic.Components;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Heretic;
using Content.Shared.Heretic.Prototypes;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.Heretic.EntitySystems;

/// <summary>
/// Handles heretic rituals and their activation on the runes
/// </summary>
public sealed partial class HereticRitualSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IAdminLogManager _adminLogManager = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly HereticKnowledgeSystem _knowledge = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    [Dependency] private readonly TransmuteBehavior _transmute = default!;
    [Dependency] private readonly TemperatureBehavior _temperature = default!;
    [Dependency] private readonly SacrificeBehavior _sacrifice = default!;
    [Dependency] private readonly ReagentPuddleBehavior _reagentPuddle = default!;
    [Dependency] private readonly MuteGhoulifyBehavior _muteGhoulify = default!;
    [Dependency] private readonly HuntAscendBehavior _huntAscend = default!;
    [Dependency] private readonly AshAscendBehavior _ashAscend = default!;

    public SoundSpecifier RitualSuccessSound = new SoundPathSpecifier("/Audio/_Goobstation/Heretic/castsummon.ogg");
    public List<EntityUid> ToDelete = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticRitualRuneComponent, InteractHandEvent>(OnInteract);
        SubscribeLocalEvent<HereticRitualRuneComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<HereticRitualRuneComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<HereticRitualRuneComponent, HereticRitualMessage>(OnRitualChosenMessage);
    }

    public HereticRitualPrototype GetRitual(ProtoId<HereticRitualPrototype>? id)
    {
        if (id == null)
            throw new ArgumentNullException();

        return _proto.Index<HereticRitualPrototype>(id);
    }

    /// <summary>
    /// Helper method for rituals.
    /// </summary>
    /// <param name="performer"></param>
    /// <param name="platform"></param>
    /// <param name="ritualId"></param>
    /// <returns></returns>
    private bool TryDoRitual(EntityUid performer, EntityUid platform, ProtoId<HereticRitualPrototype> ritualId)
    {
        if (!TryComp<HereticComponent>(performer, out var hereticComp))
            return false;

        var ritual = GetRitual(ritualId);
        var lookup = _lookup.GetEntitiesInRange(platform, .75f);

        var missingList = new List<string>();

        var requiredTags = ritual.RequiredTags?.ToDictionary(e => e.Key, e => e.Value) ?? new();

        foreach (var look in lookup)
        {
            // check for matching tags
            foreach (var tag in requiredTags)
            {
                if (!TryComp<TagComponent>(look, out var tags) // no tags?
                || _container.IsEntityInContainer(look)) // using your own eyes for amber focus?
                    continue;

                var ltags = tags.Tags;

                if (ltags.Contains(tag.Key))
                {
                    requiredTags[tag.Key] -= 1;

                    // prevent deletion of more items than needed
                    if (requiredTags[tag.Key] >= 0)
                        ToDelete.Add(look);
                }
            }
        }

        // add missing tags
        foreach (var tag in requiredTags)
        {
            if (tag.Value > 0)
                missingList.Add(tag.Key);
        }

        // are we missing anything?
        if (missingList.Count > 0)
        {
            // we are! notify the performer about that!
            var sb = new StringBuilder();
            for (var i = 0; i < missingList.Count; i++)
            {
                // makes a nice list of missing items.
                if (i != missingList.Count - 1)
                    sb.Append($"{missingList[i]}, ");
                else
                    sb.Append(missingList[i]);
            }

            _popup.PopupEntity(Loc.GetString("heretic-ritual-fail-items", ("itemlist", sb.ToString())), platform, performer);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Helper method for deleting entities on ritual success.
    /// </summary>
    public void DeleteOnSuccess()
    {
        foreach (var ent in ToDelete)
        {
            QueueDel(ent);
        }

        ToDelete = [];
    }

    /// <summary>
    /// Runs when someone clicks a rune with their empty hand
    /// </summary>
    private void OnInteract(Entity<HereticRitualRuneComponent> ent, ref InteractHandEvent args)
    {
        if (!TryComp<HereticComponent>(args.User, out var heretic))
            return;

        if (_knowledge.AllKnownRituals(heretic).Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("heretic-ritual-norituals"), args.User, args.User);
            return;
        }

        _uiSystem.OpenUi(ent.Owner, HereticRitualRuneUiKey.Key, args.User);
    }

    private void OnRitualChosenMessage(Entity<HereticRitualRuneComponent> ent, ref HereticRitualMessage args)
    {
        var user = args.Actor;

        if (!TryComp<HereticComponent>(user, out var heretic))
            return;

        heretic.ChosenRitual = args.ProtoId;

        var ritualName = Loc.GetString(GetRitual(heretic.ChosenRitual).LocName);
        _popup.PopupEntity(Loc.GetString("heretic-ritual-switch", ("name", ritualName)), user, user);
    }

    /// <summary>
    /// Handles interacting with ritual runes with an item and executing the chosen ritual.
    /// </summary>
    private void OnInteractUsing(Entity<HereticRitualRuneComponent> ent, ref InteractUsingEvent args)
    {
        if (!TryComp<HereticComponent>(args.User, out var heretic))
            return;

        if (!TryComp<MansusGraspComponent>(args.Used, out _))
            return;

        if (heretic.ChosenRitual == null)
        {
            _popup.PopupEntity(Loc.GetString("heretic-ritual-noritual"), args.User, args.User);
            return;
        }

        // Get the ritual and all of its behaviors
        var ritual = GetRitual(heretic.ChosenRitual);
        var behaviors = ritual.RitualBehavior ?? new();

        if (!TryDoRitual(args.User, ent, ritual))
            return;

        // Check all conditions are met.
        foreach (var behavior in behaviors)
        {
            // There are probably so many better ways to do this.
            switch (behavior)
            {
                // Anything that inherits from SacrificeBehavior
                // Needs to have DoRitual first to check for sacrificable bodies.
                case SacrificeBehavior:
                    if (behavior is SacrificeBehavior)
                    {
                        if (_sacrifice.DoRitual(args.User, ent, ritual) == false)
                            return;
                        break;
                    }
                    if (behavior is MuteGhoulifyBehavior)
                    {
                        if (_muteGhoulify.DoRitual(args.User, ent, ritual) == false)
                            return;
                        break;
                    }
                    if (behavior is HuntAscendBehavior)
                    {
                        if (_huntAscend.DoRitual(args.User, ent, ritual) == false && _huntAscend.DoHuntAscendRitual(args.User, ent, ritual) == false)
                            return;
                        break;
                    }
                    if (behavior is AshAscendBehavior)
                    {
                        if (_ashAscend.DoRitual(args.User, ent, ritual) == false && _ashAscend.DoAshAscendRitual(args.User, ent, ritual) == false)
                            return;
                        break;
                    }
                    break;

                case TransmuteBehavior:
                    if (_transmute.DoRitual(args.User, ent, ritual))
                        return;
                    break;

                case TemperatureBehavior:
                    if (_temperature.DoRitual(args.User, ent, ritual))
                        return;
                    break;

                case ReagentPuddleBehavior:
                    if (_reagentPuddle.DoRitual(args.User, ent, ritual))
                        return;
                    break;
            }
        }

        // Execute the ritual behaviors.
        foreach (var behavior in behaviors)
        {
            switch (behavior)
            {
                case SacrificeBehavior:
                    if (behavior is SacrificeBehavior)
                    {
                        _sacrifice.DoRitualEffect(args.User, ent, ritual);
                        break;
                    }
                    if (behavior is MuteGhoulifyBehavior)
                    {
                        _muteGhoulify.DoMuteGhoulifyRitualEffect(args.User, ent, ritual);
                        break;
                    }
                    break;

                case TransmuteBehavior:
                    _transmute.DoRitualEffect(args.User, ent, ritual);
                    break;

                case ReagentPuddleBehavior:
                    _reagentPuddle.DoRitualEffect(args.User, ent, ritual);
                    break;
            }
        }

        // Delete entities that need to be deleted.
        DeleteOnSuccess();

        // Raise the events that need to be raised, and add the knowledge that needs to be added.
        if (ritual.OutputEvent != null)
            EntityManager.EventBus.RaiseLocalEvent(args.User, ritual.OutputEvent, true);

        if (ritual.OutputKnowledge != null)
            _knowledge.AddKnowledge(args.User, heretic, (ProtoId<HereticKnowledgePrototype>)ritual.OutputKnowledge);

        // Yay yippee.
        _audio.PlayPvs(RitualSuccessSound, ent, AudioParams.Default.WithVolume(-3f));
        _popup.PopupEntity(Loc.GetString("heretic-ritual-success"), ent, args.User);
        Spawn("HereticRuneRitualAnimation", Transform(ent).Coordinates);

        // Log it.
        _adminLogManager.Add(LogType.Action, LogImpact.High, $"{args.User} performed ritual {heretic.ChosenRitual}");
    }

    private void OnExamine(Entity<HereticRitualRuneComponent> ent, ref ExaminedEvent args)
    {
        if (!TryComp<HereticComponent>(args.Examiner, out var h))
            return;

        var ritual = h.ChosenRitual != null ? GetRitual(h.ChosenRitual).LocName : null;
        var name = ritual != null ? Loc.GetString(ritual) : "None";
        args.PushMarkup(Loc.GetString("heretic-ritualrune-examine", ("rit", name)));
    }
}
