using Content.Server.Administration.Logs;
using Content.Server.Cloning;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.Database;
using Content.Shared.EntityTable;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Nutrition.AnimalHusbandry;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

using Content.Shared._Impstation.AnimalHusbandry.Components;

namespace Content.Server._Impstation.AnimalHusbandry.EntitySystems;
/// <summary>
/// System that handles mob breeding, birthing and growing
/// This system works alongside HTN
/// </summary>
public sealed partial class AnimalHusbandrySystemImp : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly CloningSystem _cloning = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly ThirstSystem _thirst = default!;
    [Dependency] private readonly EntityTableSystem _entTable = default!;

    Dictionary<EntityUid, ImpReproductiveComponent> _mobsWaiting = new Dictionary<EntityUid, ImpReproductiveComponent>();

    public override void Initialize()
    {
        base.Initialize();
    }

    /// <summary>
    /// Here we do two things
    /// 1. Loop through the pregnant mobs to see who needs to give birth
    /// 2. Track the infant mobs to see who is ready to grow up
    /// I've done this so it should be calling as minimally as possible.
    /// If there's no infants and no-one is pregnant, this basically does nothing.
    /// </summary>
    /// <param name="frameTime">Time between frames</param>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Runs through the list of pregnant mobs to see who is ready to give birth
        // or whose birth should be cancelled
        foreach (var reproComp in _mobsWaiting)
        {
            // In case the mob finds itself deleted or destroyed
            if (_entManager.Deleted(reproComp.Key)
                || !_entManager.TryGetComponent<MobStateComponent>(reproComp.Key, out var state))
            {
                _mobsWaiting.Remove(reproComp.Key);
                continue;
            }

            // If they die or become critical, the child is gone
            // Same for if the animal becomes player controlled
            if (state.CurrentState != Shared.Mobs.MobState.Alive ||
                _mind.TryGetMind(reproComp.Key, out var mind, out var mindComp))
            {
                _mobsWaiting.Remove(reproComp.Key);
                reproComp.Value.Pregnant = false;
            }

            // Is it time to give birth?
            if (reproComp.Value.EndPregnancy <= _time.CurTime)
            {
                reproComp.Value.Pregnant = false;
                Birth((reproComp.Key, reproComp.Value));
                _mobsWaiting.Remove(reproComp.Key);
                _adminLog.Add(LogType.Action, $"A mob has given birth!");
            }
        }

        // Grabs every single entity with this component and runs through them all
        // to see who should grow up.
        // Realistically there should never be THAT many infants, so this approach should be
        // fine for performance.
        var query = EntityQueryEnumerator<ImpInfantComponent>();
        while (query.MoveNext(out var uid, out var infantComp))
        {
            if (_entManager.Deleted(uid)
                || !_entManager.TryGetComponent<MobStateComponent>(uid, out var state))
                continue;

            // Doing this here for the purpose of if admins spawn in baby mobs.
            // Prevents them from growing up instantly when placed.
            if (infantComp.GrowthTimeRemaining == TimeSpan.Zero)
                infantComp.GrowthTimeRemaining = _time.CurTime.Add(infantComp.GrowthTime);

            // If the mob isn't alive we need to delay its growth
            if (state.CurrentState != Shared.Mobs.MobState.Alive)
                infantComp.GrowthTimeRemaining = infantComp.GrowthTimeRemaining.Add(_time.FrameTime);

            if (infantComp.GrowthTimeRemaining <= _time.CurTime)
                AdvanceStage((uid, infantComp));
        }
    }

    /// <summary>
    /// Handles growing an infant by deleting the current mob and making a new one
    /// This also transfers the previous components to the new mob
    /// </summary>
    /// <param name="infant">The infant that will be growing up</param>
    /// <returns></returns>
    private bool AdvanceStage(Entity<ImpInfantComponent> infant)
    {
        bool isAdult = false;

        var newStage = SpawnNewMob(infant, infant.Comp.NextStage);

        if (newStage == null)
            return false;

        if (!_prototype.Resolve(infant.Comp.OffspringSettings, out var settings))
            return false;

        // If someone is in this thing, move them over as well
        if (_mind.TryGetMind(infant, out var mind, out var mindComp))
            _mind.TransferTo(mind, newStage);

        isAdult = !_entManager.TryGetComponent<InfantComponent>(newStage, out var comp);

        // Make sure the relevant Data like damage carries over
        _cloning.CloneComponents(infant, (EntityUid)newStage, settings);

        // If there is a ghost role attached to this mob, keep it
        // We also make sure the new stage doesn't already come with one, otherwise the game crashes.
        if (_entManager.TryGetComponent<GhostRoleComponent>(infant, out var ghostComp) &&
            !_entManager.TryGetComponent<GhostRoleComponent>(newStage, out var newGhostcomp))
        {
            CopyComp(infant, (EntityUid)newStage, ghostComp);
        }

        // Pompeii Ash Baby.png
        QueueDel(infant);

        // So they don't immediately try to breed the second they grow up
        if (isAdult && _entManager.TryGetComponent<ImpReproductiveComponent>(newStage, out var reproComp))
            reproComp.NextSearch = _time.CurTime + _random.Next(reproComp.MinSearchAttemptInterval, reproComp.MaxSearchAttemptInterval);

        return isAdult;
    }

    /// <summary>
    /// Handles spawning a new mob for us
    /// </summary>
    /// <param name="entity">Entity calling the creation</param>
    /// <param name="toSpawn">What entity will be spawned</param>
    /// <returns>The ID of our new mob! Or nothing if for some reason it didn't spawn.</returns>
    public EntityUid? SpawnNewMob(EntityUid entity, EntProtoId toSpawn)
    {
        var xform = Transform(entity);

        var newMob = Spawn(toSpawn, xform.Coordinates);

        return newMob;
    }
}
