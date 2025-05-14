using Content.Shared.Damage.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Physics.Components;
using CCVars = Content.Shared._EE.CCVar.EECCVars; // Frontier

namespace Content.Shared._EE.Contests;

public sealed partial class ContestsSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    /// <summary>
    ///     The presumed average mass of a player entity
    ///     Defaulted to the average mass of an adult human
    /// </summary>

    #region Mass Contests

    /// <summary>
    ///     Outputs the ratio of mass between a performer and a target
    /// </summary>
    public float MassContest(Entity<PhysicsComponent?> performer, Entity<PhysicsComponent?> target, float rangeFactor = 1f)
    {
        if (!CheckCVars("Mass")
            || !Resolve(performer, ref performer.Comp)
            || !Resolve(target, ref target.Comp)
            || performer.Comp.Mass == 0
            || target.Comp.InvMass == 0)
            return 1f;

        return ContestClamp(Math.Clamp(performer.Comp.Mass * target.Comp.InvMass,
                1 - _cfg.GetCVar(CCVars.MassContestsMaxPercentage) * rangeFactor,
                1 + _cfg.GetCVar(CCVars.MassContestsMaxPercentage) * rangeFactor));
    }

    #endregion

    #region Stamina Contests

    /// <summary>
    ///     Outputs 1 minus the percentage of an Entity's Stamina, with a Range of [Epsilon, 1 - 0.25 * rangeFactor].
    ///     This will never return a value >1.
    /// </summary>
    public float StaminaContest(Entity<StaminaComponent?> performer, float rangeFactor = 1f)
    {
        if (!Resolve(performer, ref performer.Comp)
            || performer.Comp.StaminaDamage == 0
            || !CheckCVars("Stamina"))
            return 1f;

        return ContestClamp(1 - Math.Clamp(performer.Comp.StaminaDamage
            / performer.Comp.CritThreshold, 0, 0.25f * rangeFactor));
    }

    /// <summary>
    ///     Outputs the ratio of percentage of an Entity's Stamina and a Target Entity's Stamina, with a Range of [Epsilon, 0.25 * rangeFactor], or a range of [Epsilon, +inf] if bypassClamp is true.
    ///     This does NOT produce the same kind of outputs as a Single-Entity StaminaContest. 2Entity StaminaContest returns the product of two Solo Stamina Contests, and so its values can be very strange.
    /// </summary>
    public float StaminaContest(Entity<StaminaComponent?> performer, Entity<StaminaComponent?> target, float rangeFactor = 1f)
    {
        if (!CheckCVars("Stamina")
            || !Resolve(performer, ref performer.Comp)
            || !Resolve(target, ref target.Comp))
            return 1f;

        return ContestClamp((1 - Math.Clamp(performer.Comp.StaminaDamage / performer.Comp.CritThreshold, 0, 0.25f * rangeFactor))
                / (1 - Math.Clamp(target.Comp.StaminaDamage / target.Comp.CritThreshold, 0, 0.25f * rangeFactor)));
    }

    #endregion

    #region helpers

    /// <summary>
    /// Eligible values: Mass, Stamina
    /// </summary>
    private bool CheckCVars(string checkType)
    {
        if (!_cfg.GetCVar(CCVars.DoContestsSystem))
            return false;
        return checkType switch
        {
            "Mass" => CheckMassCVars(),
            "Stamina" => CheckStaminaCVars(),
            _ => true,
        };
    }

    private bool CheckMassCVars()
    {
        if (!_cfg.GetCVar(CCVars.DoMassContests))
            return false;
        return true;
    }

    private bool CheckStaminaCVars()
    {
        if (!_cfg.GetCVar(CCVars.DoStaminaContests))
            return false;
        return true;
    }

    #endregion
}
