using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Actions;
using Content.Server.Chat.Managers;
using Content.Shared._Impstation.StrangeMoods;
using Content.Shared.Chat;
using Content.Shared.Dataset;
using Content.Shared.GameTicking;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;

namespace Content.Server._Impstation.StrangeMoods;

public sealed class StrangeMoodsSystem : SharedStrangeMoodsSystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly UserInterfaceSystem _bui = default!;

    private readonly HashSet<SharedMood> _sharedMoods = [];
    private readonly HashSet<StrangeMood> _emptyMoods = []; // cached hashset that never gets modified
    private readonly HashSet<ProtoId<StrangeMoodPrototype>> _moodProtos = []; // cached hashset that gets changed in GetMoodProtoSet

    public override void Initialize()
    {
        base.Initialize();

        NewSharedMoods();

        SubscribeLocalEvent<StrangeMoodsComponent, MapInitEvent>(OnStrangeMoodsInit);
        SubscribeLocalEvent<StrangeMoodsComponent, ComponentShutdown>(OnStrangeMoodsShutdown);
        SubscribeLocalEvent<StrangeMoodsComponent, ToggleMoodsScreenEvent>(OnToggleMoodsScreen);
        SubscribeLocalEvent<StrangeMoodsComponent, BoundUIOpenedEvent>(OnBoundUIOpened);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => NewSharedMoods());
    }

    private void OnStrangeMoodsInit(Entity<StrangeMoodsComponent> ent, ref MapInitEvent args)
    {
        var mood = ent.Comp.StrangeMood;
        var moodProto = _proto.Index(ent.Comp.StrangeMoodPrototype);
        _serialization.CopyTo(moodProto, ref mood, notNullableOverride: true);

        RefreshMoods(ent);
        SetSharedMood(ent, mood.SharedMoodPrototype);

        // Add any required components
        if (moodProto.Components is { } components)
        {
            EntityManager.AddComponents(ent, components);
        }

        // Add action to bar
        ent.Comp.Action ??= _actions.AddAction(ent.Owner, mood.ActionViewMoods);
        if (TryComp<UserInterfaceComponent>(ent, out var ui))
            _bui.SetUi((ent, ui), StrangeMoodsUiKey.Key, new InterfaceData("StrangeMoodsBoundUserInterface", requireInputValidation: false));
    }

    private void OnStrangeMoodsShutdown(Entity<StrangeMoodsComponent> ent, ref ComponentShutdown args)
    {
        var moodProto = _proto.Index(ent.Comp.StrangeMoodPrototype);

        // Remove any added components
        if (moodProto.Components is { } components)
        {
            EntityManager.RemoveComponents(ent, components);
        }

        // Remove action
        _actions.RemoveAction(ent.Owner, ent.Comp.Action);
    }

    private void OnToggleMoodsScreen(Entity<StrangeMoodsComponent> ent, ref ToggleMoodsScreenEvent args)
    {
        if (args.Handled || !TryComp<ActorComponent>(ent, out var actor) ||
            !_bui.TryToggleUi(ent.Owner, StrangeMoodsUiKey.Key, actor.PlayerSession))
            return;

        args.Handled = true;
    }

    private void OnBoundUIOpened(Entity<StrangeMoodsComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateBuiState(ent);
    }

    #region Helper functions

    /// <summary>
    /// Clears the moods for an entity, then applies a new set of moods.
    /// </summary>
    public void RefreshMoods(Entity<StrangeMoodsComponent> ent, Dictionary<ProtoId<DatasetPrototype>, int>? datasets = null)
    {
        datasets ??= ent.Comp.StrangeMood.Datasets;
        ent.Comp.StrangeMood.Moods = _emptyMoods.ToList();

        foreach (var moodset in datasets)
        {
            var dataset = moodset.Key;
            var count = moodset.Value;

            for (var i = 0; i < count; i++)
            {
                if (TryPick(dataset, out var mood, GetActiveMoods(ent)))
                    TryAddMood(ent, mood, true, false);
            }
        }
    }

    /// <summary>
    /// Checks if the given mood prototype conflicts with the current moods, and
    /// adds the mood if it does not.
    /// </summary>
    public bool TryAddMood(Entity<StrangeMoodsComponent> ent, StrangeMoodPrototype moodProto, bool allowConflict = false, bool notify = true)
    {
        if (!allowConflict && GetConflicts(ent).Contains(moodProto.ID))
            return false;

        AddMood(ent, RollMood(moodProto), notify);
        return true;
    }

    /// <summary>
    /// Tries to add a random mood using a specific dataset.
    /// </summary>
    public bool TryAddRandomMood(Entity<StrangeMoodsComponent> ent, ProtoId<DatasetPrototype> dataset, bool notify = true, HashSet<ProtoId<StrangeMoodPrototype>>? conflicts = null)
    {
        conflicts ??= [];
        conflicts.UnionWith(GetConflicts(ent));

        if (!TryPick(dataset, out var moodProto, GetActiveMoods(ent), conflicts))
            return false;

        AddMood(ent, RollMood(moodProto), notify);
        return true;
    }

    /// <summary>
    /// Tries to add a random mood using a weighted random dataset.
    /// </summary>
    public bool TryAddRandomMood(Entity<StrangeMoodsComponent> ent, ProtoId<WeightedRandomPrototype> dataset, bool notify = true, HashSet<ProtoId<StrangeMoodPrototype>>? conflicts = null)
    {
        var chosenDataset = _proto.Index(dataset).Pick();

        if (!_proto.Resolve(chosenDataset, out DatasetPrototype? proto))
            return false;

        return TryAddRandomMood(ent, proto, notify, conflicts);
    }

    /// <summary>
    /// Set the moods for an entity directly.
    /// This does NOT check conflicts so be careful with what you set!
    /// </summary>
    public void SetMoods(Entity<StrangeMoodsComponent> ent, IEnumerable<StrangeMood> moods, bool notify = true)
    {
        ent.Comp.StrangeMood.Moods = moods.ToList();
        Dirty(ent);

        if (notify)
            NotifyMoodChange(ent);
        else
            UpdateBuiState(ent);
    }

    /// <summary>
    /// Sends the player an audiovisual notification and updates the moods UI.
    /// </summary>
    public void NotifyMoodChange(Entity<StrangeMoodsComponent> ent)
    {
        if (!TryComp<ActorComponent>(ent, out var actor))
            return;

        var session = actor.PlayerSession;
        _audio.PlayGlobal(ent.Comp.StrangeMood.MoodsChangedSound, session);

        var msg = Loc.GetString(ent.Comp.StrangeMood.MoodsChangedMessage);
        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
        _chat.ChatMessageToOne(ChatChannel.Server, msg, wrappedMessage, default, false, session.Channel, colorOverride: ent.Comp.StrangeMood.MoodsChangedColor);

        UpdateBuiState(ent); // update the UI without needing to re-open it
    }

    /// <summary>
    /// Directly add a mood to an entity, ignoring conflicts.
    /// </summary>
    private void AddMood(Entity<StrangeMoodsComponent> ent, StrangeMood mood, bool notify = true)
    {
        ent.Comp.StrangeMood.Moods.Add(mood);
        Dirty(ent);

        if (notify)
            NotifyMoodChange(ent);
        else // NotifyMoodChange will update UI so this is in else
            UpdateBuiState(ent);
    }

    /// <summary>
    /// Attempts to pick a new mood from the given dataset.
    /// </summary>
    private bool TryPick(ProtoId<DatasetPrototype> dataset, [NotNullWhen(true)] out StrangeMoodPrototype? proto, IEnumerable<StrangeMood>? currentMoods = null, HashSet<ProtoId<StrangeMoodPrototype>>? conflicts = null)
    {
        var choices = _proto.Index(dataset).Values.ToList();

        currentMoods ??= _emptyMoods;
        conflicts ??= GetConflicts(currentMoods);

        var currentMoodProtos = GetMoodProtoSet(currentMoods);

        while (choices.Count > 0)
        {
            var moodId = _random.PickAndTake(choices);

            if (conflicts.Contains(moodId))
                continue; // Skip proto if an existing mood conflicts with it

            var moodProto = _proto.Index<StrangeMoodPrototype>(moodId);

            if (moodProto.Conflicts.Overlaps(currentMoodProtos))
                continue; // Skip proto if it conflicts with an existing mood

            proto = moodProto;
            return true;
        }

        proto = null;
        return false;
    }

    /// <summary>
    /// Attempts to get a <see cref="SharedMoodPrototype" />'s relevant shared mood from <see cref="_sharedMoods" />.
    /// </summary>
    private bool TryGetSharedMood(SharedMoodPrototype proto, out SharedMood? sharedMood)
    {
        foreach (var mood in _sharedMoods.Where(mood => proto.ProtoId == mood.ProtoId))
        {
            sharedMood = mood;
            return true;
        }

        sharedMood = null;
        return false;
    }

    /// <summary>
    /// Updates the mood UI.
    /// </summary>
    private void UpdateBuiState(Entity<StrangeMoodsComponent> ent)
    {
        var moods = ent.Comp.SharedMood is { } sharedMood
            ? sharedMood.Moods
            : [];

        var state = new StrangeMoodsBuiState(moods);
        _bui.SetUiState(ent.Owner, StrangeMoodsUiKey.Key, state);
    }

    /// <summary>
    /// Creates a StrangeMood instance from the given StrangeMoodPrototype, and rolls
    /// its mood vars.
    /// </summary>
    public StrangeMood RollMood(StrangeMoodPrototype proto)
    {
        var mood = new StrangeMood();
        _serialization.CopyTo(proto, ref mood, notNullableOverride: true);
        var alreadyChosen = new HashSet<ProtoId<StrangeMoodPrototype>>();

        foreach (var (name, datasetId) in proto.MoodVarDatasets)
        {
            var dataset = _proto.Index(datasetId);

            if (proto.AllowDuplicateMoodVars)
            {
                mood.MoodVars.Add(name, _random.Pick(dataset.Values));
                continue;
            }

            var choices = dataset.Values.ToList();
            var foundChoice = false;

            while (choices.Count > 0)
            {
                var choice = _random.PickAndTake(choices);
                if (alreadyChosen.Contains(choice) || mood.MoodVars.ContainsValue(choice))
                    continue;

                mood.MoodVars.TryAdd(name, choice);
                alreadyChosen.Add(choice);
                foundChoice = true;
                break;
            }

            if (!foundChoice)
                Log.Warning($"Ran out of choices for moodvar \"{name}\" in \"{proto.ID}\"!");
        }

        return mood;
    }

    /// <summary>
    /// Get the conflicts for an entity's active moods.
    /// </summary>
    private static HashSet<ProtoId<StrangeMoodPrototype>> GetConflicts(IEnumerable<StrangeMood> moods)
    {
        var conflicts = new HashSet<ProtoId<StrangeMoodPrototype>>();

        foreach (var mood in moods)
        {
            if (mood.ProtoId is { } id)
                conflicts.Add(id); // Specific moods shouldn't be added twice

            conflicts.UnionWith(mood.Conflicts);
        }

        return conflicts;
    }

    /// <summary>
    /// Get the conflicts for an entity's active moods.
    /// </summary>
    private HashSet<ProtoId<StrangeMoodPrototype>> GetConflicts(Entity<StrangeMoodsComponent> ent)
    {
        // TODO: Should probably cache this when moods get updated
        return GetConflicts(GetActiveMoods(ent));
    }

    /// <summary>
    /// Maps some moods to their IDs.
    /// The hashset returned is reused and so you must not modify it.
    /// </summary>
    private HashSet<ProtoId<StrangeMoodPrototype>> GetMoodProtoSet(IEnumerable<StrangeMood> moods)
    {
        _moodProtos.Clear();

        foreach (var mood in moods)
        {
            if (mood.ProtoId is { } id)
                _moodProtos.Add(id);
        }

        return _moodProtos;
    }

    /// <summary>
    /// Return a list of the moods that are affecting this entity.
    /// </summary>
    private List<StrangeMood> GetActiveMoods(Entity<StrangeMoodsComponent> ent, bool includeShared = true)
    {
        if (includeShared && ent.Comp.SharedMood is { } sharedMood)
        {
            return [..sharedMood.Moods.Concat(ent.Comp.StrangeMood.Moods)];
        }

        return ent.Comp.StrangeMood.Moods;
    }

    #endregion

    #region Shared mood helper functions

    /// <summary>
    /// Generates new moods for all shared moods.
    /// </summary>
    private void NewSharedMoods()
    {
        _sharedMoods.Clear();

        var sharedMoods = _proto.EnumeratePrototypes<SharedMoodPrototype>();

        foreach (var mood in sharedMoods)
        {
            for (var i = 0; i < mood.Count; i++)
            {
                TryAddSharedMood(mood, notify: false);
            }

            NotifySharedMoodChange(mood);
        }
    }

    /// <summary>
    /// Generates new moods for the given shared mood.
    /// </summary>
    public void NewSharedMoods(SharedMood sharedMood)
    {
        _sharedMoods.Remove(sharedMood);

        for (var i = 0; i < sharedMood.Count; i++)
        {
            TryAddSharedMood(sharedMood, notify: false);
        }

        NotifySharedMoodChange(sharedMood);
    }

    /// <summary>
    /// Generates new moods for the given shared mood.
    /// </summary>
    public void NewSharedMoods(ProtoId<SharedMoodPrototype> sharedMood)
    {
        if (!TryGetSharedMood(_proto.Index(sharedMood), out var mood) || mood == null)
            return;

        NewSharedMoods(mood);
    }

    /// <summary>
    /// Attempts to add a mood to the given shared mood.
    /// If no mood is specified, a random mood is added.
    /// </summary>
    private bool TryAddSharedMood(SharedMood sharedMood, StrangeMood? newMood = null, bool checkConflicts = true, bool notify = true)
    {
        var mood = new SharedMood();
        _serialization.CopyTo(sharedMood, ref mood, notNullableOverride: true);

        if (newMood == null)
        {
            if (!TryPick(mood.Dataset, out var moodProto, mood.Moods))
                return false;

            newMood = RollMood(moodProto);
            checkConflicts = false; // TryPick has cleared this mood already
        }

        if (checkConflicts && SharedMoodConflicts(mood, newMood))
            return false;

        mood.Moods.Add(newMood);

        if (notify)
            NotifySharedMoodChange(mood);

        _sharedMoods.Add(mood);
        return true;
    }

    /// <summary>
    /// Sets which shared moods an entity should follow.
    /// If null, the entity will not follow any shared moods.
    /// </summary>
    public void SetSharedMood(Entity<StrangeMoodsComponent> ent, ProtoId<SharedMoodPrototype>? newMood)
    {
        if (newMood == null ||
            !TryGetSharedMood(_proto.Index(newMood), out var sharedMood))
        {
            ent.Comp.SharedMood = null;
            return;
        }

        var mood = new SharedMood();
        _serialization.CopyTo(sharedMood, ref mood);
        ent.Comp.SharedMood = mood;
    }

    /// <summary>
    ///
    /// </summary>
    private bool SharedMoodConflicts(SharedMood sharedMood, StrangeMood mood)
    {
        return mood.ProtoId is { } id &&
            (GetConflicts(sharedMood.Moods).Contains(id) ||
            GetMoodProtoSet(sharedMood.Moods).Overlaps(mood.Conflicts));
    }

    /// <summary>
    /// Sends a "moods changed" alert to all entities with the same shared mood.
    /// </summary>
    private void NotifySharedMoodChange(SharedMood sharedMood)
    {
        var query = EntityQueryEnumerator<StrangeMoodsComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.SharedMood == null ||
                comp.SharedMood.ProtoId != sharedMood.ProtoId)
                continue;

            NotifyMoodChange((uid, comp));
        }
    }

    #endregion
}
