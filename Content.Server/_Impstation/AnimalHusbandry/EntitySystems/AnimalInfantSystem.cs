using Content.Server.Administration.Logs;
using Content.Server.Cloning;
using Content.Server.Ghost.Roles.Components;
using Content.Shared._Impstation.AnimalHusbandry.Components;
using Content.Shared._Impstation.CCVar;
using Content.Shared.Database;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Impstation.AnimalHusbandry.EntitySystems;

/// <summary>
///     This system handles the growth and advancement of infant mobs.
/// </summary>
public sealed partial class AnimalInfantSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly AnimalHusbandrySystemImp _animalHusbandry = default!;
    [Dependency] private readonly CloningSystem _cloning = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IGameTiming _time = default!;

    private TimeSpan _lastUpdated = TimeSpan.Zero;
    public TimeSpan UpdateRate = TimeSpan.FromSeconds(1.0f);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobStateComponent, InfantCanGrowEvent>(CanMobStateGrow);

        Subs.CVar(_config,
            ImpCCVars.AnimalHusbandryUpdateInterval,
            value => UpdateRate = TimeSpan.FromSeconds(value),
            invokeImmediately: true);
    }

    /// <summary>
    ///     Advance the growth of all infants.
    ///     This happens on an interval loop for performance reasons.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // We only update pregnancy and growth timers every [interval], for performance.
        if (_time.CurTime < _lastUpdated + UpdateRate)
            return;

        // Time elapsed since last update. May be slightly more than [interval], depending on frame time.
        var growthTime = _time.CurTime - _lastUpdated;
        _lastUpdated = _time.CurTime;

        // Advance the growth of all infants.
        var infantQuery = EntityQueryEnumerator<ImpInfantComponent>();
        while (infantQuery.MoveNext(out var uid, out var infant))
        {
            // Account for situations in which the infant is unable to grow.
            if (!CanGrow((uid, infant)))
                continue;

            infant.CurrentGrowthTime += growthTime;

            // Advance if the infant is done growing.
            if (infant.CurrentGrowthTime >= infant.GrowthTime)
                AdvanceStage((uid, infant));
        }
    }

    /// <summary>
    ///     Gets whether or not an infant is currently capable of growing.
    /// </summary>
    /// <param name="ent">The infant entity.</param>
    /// <returns>Whether or not an infant is currently capable of growing.</returns>
    public bool CanGrow(Entity<ImpInfantComponent?> ent)
    {
        // Not an infant.
        if (!Resolve(ent.Owner, ref ent.Comp, logMissing: false))
            return false;

        // Being deleted.
        if (TerminatingOrDeleted(ent))
            return false;

        var ev = new InfantCanGrowEvent();
        RaiseLocalEvent(ent.Owner, ref ev);

        return !ev.Cancelled;
    }

    /// <summary>
    ///     Prevents infant growth if this mob is critical or dead.
    /// </summary>
    /// <param name="ent">The mob state entity.</param>
    private void CanMobStateGrow(Entity<MobStateComponent> ent, ref InfantCanGrowEvent args)
    {
        if (ent.Comp.CurrentState != Shared.Mobs.MobState.Alive)
            args.Cancelled = true;
    }

    /// <summary>
    /// Handles growing an infant by deleting the current mob and making a new one
    /// This also transfers the previous components to the new mob
    /// </summary>
    /// <param name="infant">The infant that will be growing up</param>
    /// <returns></returns>
    private bool AdvanceStage(Entity<ImpInfantComponent> infant)
    {
        var newStage = _animalHusbandry.SpawnOnTop(infant, infant.Comp.NextStage);

        if (!_prototype.Resolve(infant.Comp.OffspringSettings, out var settings))
            return false;

        // If someone is in this thing, move them over as well
        if (_mind.TryGetMind(infant, out var mind, out var mindComp))
            _mind.TransferTo(mind, newStage);

        // Make sure the relevant Data like damage carries over
        _cloning.CloneComponents(infant, newStage, settings);

        // If there is a ghost role attached to this mob, try to keep it
        if (TryComp<GhostRoleComponent>(infant, out var ghostComp))
            TryCopyComponent(infant, newStage, ref ghostComp, out var _);

        _adminLog.Add(LogType.Action,
            $"{ToPrettyString(infant)} advanced to next growth stage: {ToPrettyString(newStage)}");

        // Pompeii Ash Baby.png
        QueueDel(infant);

        var isAdult = HasComp<ImpInfantComponent>(infant.Owner);

        // So they don't immediately try to breed the second they grow up
        if (isAdult)
            _animalHusbandry.RefreshSearchTime(newStage);

        return isAdult;
    }
}
