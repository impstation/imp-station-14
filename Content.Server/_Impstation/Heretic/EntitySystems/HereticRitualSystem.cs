using System.Linq;
using System.Text;
using Content.Server._Impstation.Heretic.Ritual;
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
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IAdminLogManager _adminLogManager = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly HereticKnowledgeSystem _knowledge = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    // Behavior systems
    [Dependency] private readonly TransmuteBehaviorSystem _transmute = default!;
    [Dependency] private readonly TemperatureBehaviorSystem _temperature = default!;
    [Dependency] private readonly SacrificeBehaviorSystem _sacrifice = default!;
    [Dependency] private readonly ReagentPuddleBehaviorSystem _reagentPuddle = default!;
    [Dependency] private readonly MuteGhoulifyBehaviorSystem _muteGhoulify = default!;
    [Dependency] private readonly HuntAscendBehaviorSystem _huntAscend = default!;
    [Dependency] private readonly AshAscendBehaviorSystem _ashAscend = default!;

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
    /// Runs when someone clicks a rune with their empty hand
    /// </summary>
    private void OnInteract(Entity<HereticRitualRuneComponent> ent, ref InteractHandEvent args)
    {
        if (!TryComp<HereticComponent>(args.User, out _))
            return;

        _uiSystem.OpenUi(ent.Owner, HereticRitualRuneUiKey.Key, args.User);
    }

    private void OnRitualChosenMessage(Entity<HereticRitualRuneComponent> ent, ref HereticRitualMessage args)
    {
        if (!TryComp<HereticComponent>(args.Actor, out var hereticComp))
            return;

        hereticComp.ChosenRitual = GetRitual(args.ProtoId);
        var ritualName = Loc.GetString(GetRitual(args.ProtoId).LocName);
        _popup.PopupEntity(Loc.GetString("heretic-ritual-switch", ("name", ritualName)), args.Actor, args.Actor);
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

        // Check that we have all of the items needed.
        if (!TryDoRitual(args.User, ent, ritual))
            return;

        // Check all other conditions are met.
        // There are probably better ways to do this.
        if (ritual.Components.TryGetComponent<AshAscendBehaviorComponent>(_factory, out var ashComp))
        {
            if (!_ashAscend.DoRitual(args.User, ent, ashComp) || _ashAscend.DoAshAscendRitual(args.User, ent, ashComp))
                return;
        }
        else if (ritual.Components.TryGetComponent<HuntAscendBehaviorComponent>(_factory, out var huntComp))
        {
            if (!_huntAscend.DoRitual(args.User, ent, huntComp) || _huntAscend.DoHuntAscendRitual(args.User, ent, huntComp))
                return;
        }
        else if (ritual.Components.TryGetComponent<MuteGhoulifyBehaviorComponent>(_factory, out var muteComp))
        {
            if (!_muteGhoulify.DoRitual(args.User, ent, muteComp))
                return;
        }
        else if (ritual.Components.TryGetComponent<ReagentPuddleBehaviorComponent>(_factory, out var reagentComp))
        {
            if (!_reagentPuddle.DoRitual(args.User, ent, reagentComp))
                return;
        }
        else if (ritual.Components.TryGetComponent<SacrificeBehaviorComponent>(_factory, out var sacrificeComp))
        {
            if (!_sacrifice.DoRitual(args.User, ent, sacrificeComp))
                return;
        }
        else if (ritual.Components.TryGetComponent<TemperatureBehaviorComponent>(_factory, out var tempComp))
        {
            if (!_temperature.DoRitual(args.User, ent, tempComp))
                return;
        }

        // Execute the ritual behaviors.
        if (ritual.Components.TryGetComponent<AshAscendBehaviorComponent>(_factory, out _))
        {
            // I don't entirely understand why the earlier ashComp is in scope here. It's Concerning.
            _ashAscend.DoRitualEffect(args.User, ashComp ?? new());
        }
        else if (ritual.Components.TryGetComponent<HuntAscendBehaviorComponent>(_factory, out var huntComp))
        {
            _huntAscend.DoRitualEffect(args.User, huntComp ?? new());
        }
        else if (ritual.Components.TryGetComponent<MuteGhoulifyBehaviorComponent>(_factory, out _))
        {
            _muteGhoulify.DoMuteGhoulifyRitualEffect(args.User);
        }
        else if (ritual.Components.TryGetComponent<ReagentPuddleBehaviorComponent>(_factory, out _))
        {
            _reagentPuddle.DoRitualEffect();
        }
        else if (ritual.Components.TryGetComponent<SacrificeBehaviorComponent>(_factory, out var sacrificeComp))
        {
            _sacrifice.DoRitualEffect(args.User, sacrificeComp);
        }
        else if (ritual.Components.TryGetComponent<TransmuteBehaviorComponent>(_factory, out var transComp))
        {
            _transmute.DoRitualEffect(args.User, args.Target, transComp);
        }

        // Delete entities that need to be deleted.
        foreach (var entity in ToDelete)
        {
            QueueDel(entity);
        }

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

        // Reset ToDelete.
        ToDelete = [];
    }

    /// <summary>
    /// Event called when the ritual rune is examined.
    /// </summary>
    private void OnExamine(Entity<HereticRitualRuneComponent> ent, ref ExaminedEvent args)
    {
        if (!TryComp<HereticComponent>(args.Examiner, out var hereticComp))
            return;

        var ritual = hereticComp.ChosenRitual != null ? GetRitual(hereticComp.ChosenRitual).LocName : null;
        var name = ritual != null ? Loc.GetString(ritual) : Loc.GetString("heretic-ritual-rune-no-ritual");

        args.PushMarkup(Loc.GetString("heretic-ritual-rune-examine", ("ritual", name)));
    }
}
