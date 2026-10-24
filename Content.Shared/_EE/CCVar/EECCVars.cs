using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared._EE.CCVar;

[CVarDefs]
public sealed partial class EECCVars : CVars
{
    // TODO: Move the rest of the announcer code to _EE

    #region Announcers

    /// <summary>
    ///     Weighted list of announcers to choose from
    /// </summary>
    public static readonly CVarDef<string> AnnouncerList =
        CVarDef.Create("announcer.list", "RandomAnnouncers", CVar.REPLICATED);

    /// <summary>
    ///     Optionally force set an announcer
    /// </summary>
    public static readonly CVarDef<string> Announcer =
        CVarDef.Create("announcer.announcer", "", CVar.SERVERONLY);

    /// <summary>
    ///     Optionally blacklist announcers
    ///     List of IDs separated by commas
    /// </summary>
    public static readonly CVarDef<string> AnnouncerBlacklist =
        CVarDef.Create("announcer.blacklist", "", CVar.SERVERONLY);

    /// <summary>
    ///     Changes how loud the announcers are for the client
    /// </summary>
    public static readonly CVarDef<float> AnnouncerVolume =
        CVarDef.Create("announcer.volume", 0.5f, CVar.ARCHIVE | CVar.CLIENTONLY);

    /// <summary>
    ///     Disables multiple announcement sounds from playing at once
    /// </summary>
    public static readonly CVarDef<bool> AnnouncerDisableMultipleSounds =
        CVarDef.Create("announcer.disable_multiple_sounds", false, CVar.ARCHIVE | CVar.CLIENTONLY);

    #endregion

    #region Contests System

    /// <summary>
    ///     The MASTER TOGGLE for the entire Contests System.
    ///     ALL CONTESTS BELOW, regardless of type or setting will output 1f when false.
    /// </summary>
    public static readonly CVarDef<bool> DoContestsSystem =
        CVarDef.Create("contests.do_contests_system", true, CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    ///     Toggles all MassContest functions. All mass contests output 1f when false
    /// </summary>
    public static readonly CVarDef<bool> DoMassContests =
        CVarDef.Create("contests.do_mass_contests", true, CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    ///     Toggles all StaminaContest functions. All stamina contests output 1f when false
    /// </summary>
    public static readonly CVarDef<bool> DoStaminaContests =
        CVarDef.Create("contests.do_stamina_contests", true, CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    ///     The maximum amount that Contests can modify a physics multiplier, given as a +/- percentage
    ///     Default of 0.25f outputs between * 0.75f and 1.25f
    /// </summary>
    public static readonly CVarDef<float> ContestsMaxPercentage =
        CVarDef.Create("contests.max_percentage", 0.25f, CVar.REPLICATED | CVar.SERVER);

    // FRONTIER EDITS:
    /// <summary>
    /// base throwing speed reduction
    /// </summary>
    public static readonly CVarDef<float> BaseDistanceCoeff =
        CVarDef.Create("contests.base_distance_coeff", 0.5f, CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    /// max throwing speed reduction
    /// </summary>
    public static readonly CVarDef<float> MaxDistanceCoeff =
        CVarDef.Create("contests.max_distance_coeff", 1.0f, CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    /// max throw distance
    /// </summary>
    public static readonly CVarDef<float> DefaultMaxThrowDistance =
        CVarDef.Create("contests.default_max_throw_distance", 4.0f, CVar.REPLICATED | CVar.SERVER);

    // imp add
    /// <summary>
    ///     Coefficient to modify the speed at which entities escape from being carried
    /// </summary>
    public static readonly CVarDef<float> CarryEscapeCoeff =
        CVarDef.Create("contests.carry_escape_coeff", 0.5f, CVar.REPLICATED | CVar.SERVER);

    #endregion
}
