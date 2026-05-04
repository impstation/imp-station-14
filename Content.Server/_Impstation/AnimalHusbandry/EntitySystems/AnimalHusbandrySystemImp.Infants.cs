using Content.Server.Ghost.Roles.Components;
using Content.Shared._Impstation.AnimalHusbandry.Components;
using Content.Shared.Database;
using Content.Shared.Mobs.Components;

namespace Content.Server._Impstation.AnimalHusbandry.EntitySystems;

public sealed partial class AnimalHusbandrySystemImp : EntitySystem
{
    /// <summary>
    ///     Gets whether or not an infant is currently capable of growing.
    /// </summary>
    /// <param name="ent">The infant entity.</param>
    /// <returns>Whether or not an infant is currently capable of growing.</returns>
    public bool CanGrow(Entity<ImpInfantComponent?> ent)
    {
        // Not an infant.
        if (!Resolve(ent.Owner, ref ent.Comp))
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
        var newStage = SpawnOnTop(infant, infant.Comp.NextStage);

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
            RefreshSearchTime(newStage);

        return isAdult;
    }
}
