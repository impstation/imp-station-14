using Content.Shared._Impstation.Supermatter.Components;
using Content.Shared.Atmos;
using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared._Impstation.CCVar;

// ReSharper disable once InconsistentNaming
[CVarDefs]
public sealed class ImpCCVars : CVars
{
    /// <summary>
    /// Toggles the proximity warping effect on the singularity.
    /// This option is for people who generally do not mind motion, but find
    /// the singularity warping especially egregious.
    /// </summary>
    public static readonly CVarDef<bool> DisableSinguloWarping =
        CVarDef.Create("accessibility.disable_singulo_warping", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Toggles the effects of weather on the client.
    /// This is a toggle because it is a photosensitivity concern.
    /// Please keep that in mind if you are touching this in the future.
    /// </summary>
    public static readonly CVarDef<bool> DisableWeather =
        CVarDef.Create("accessibility.disable_weather", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// </summary>
    /// Replaces the AI static camera effect with a plain black gradient.
    /// </summary>
    public static readonly CVarDef<bool> DisableAiStatic =
        CVarDef.Create("accessibility.disable_ai_static", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// </summary>
    /// Makes the Biomagnetic Polarization status effect polarity show as a large symbol ontop of the entity.
    /// </summary>
    public static readonly CVarDef<bool> EnableBiomagneticPolarizationSymbols =
        CVarDef.Create("accessibility.enable_biomagnetic_polarization_symbols", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// The number of shared moods to give thaven by default.
    /// </summary>
    public static readonly CVarDef<uint> ThavenSharedMoodCount =
        CVarDef.Create<uint>("thaven.shared_mood_count", 1, CVar.SERVERONLY);

    /// <summary>
    /// URL of the Discord webhook which will relay last messages before death.
    /// </summary>
    public static readonly CVarDef<string> DiscordLastMessageBeforeDeathWebhook =
        CVarDef.Create("discord.last_message_before_death_webhook", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    /// A maximum length before an IC message is cut off in LastMessageBeforeDeathSystem during formatting.
    /// Can't be less than 1.
    /// Do not set this value above 2000, as that is the limit for discord webhook messages
    /// </summary>
    public static readonly CVarDef<int> DiscordLastMessageSystemMaxICLength =
        CVarDef.Create("discord.last_message_system_max_ic_length", 2000, CVar.SERVERONLY);

    /// <summary>
    /// A maximum length of a discord message that a webhook sends.
    /// Can't be more than 2000 and can't be less than 1.
    /// </summary>
    public static readonly CVarDef<int> DiscordLastMessageSystemMaxMessageLength =
        CVarDef.Create("discord.last_message_system_max_message_length", 2000, CVar.SERVERONLY);

    /// <summary>
    /// A maximum amount of a discord messages that a webhook sends in one batch.
    /// </summary>
    public static readonly CVarDef<int> DiscordLastMessageSystemMaxMessageBatch =
        CVarDef.Create("discord.last_message_system_max_message_batch", 15, CVar.SERVERONLY);

    /// <summary>
    /// Delay in milliseconds between each message the discord webhook sends.
    /// </summary>
    public static readonly CVarDef<int> DiscordLastMessageSystemMessageDelay =
        CVarDef.Create("discord.last_message_system_message_delay", 2000, CVar.SERVERONLY);

    /// <summary>
    /// If a maximum amount of messages per batch has been reached, we wait this amount of time (in milliseconds) to send what's left.
    /// </summary>
    public static readonly CVarDef<int> DiscordLastMessageSystemMaxMessageBatchOverflowDelay =
        CVarDef.Create("discord.last_message_system_max_message_batch_overflow_delay", 60000, CVar.SERVERONLY);

    /// <summary>
    ///     If true, antag selection will prioritize players with less antag time.
    /// </summary>
    public static readonly CVarDef<bool> AntagPlaytimeBiasing =
        CVarDef.Create("antag.play_time_biasing", false, CVar.SERVERONLY);

    /// <summary>
    /// How many characters the notifier text can be.
    /// </summary>
    public static readonly CVarDef<int> NotifierFreetextMaxLength =
        CVarDef.Create("notifier.freetext_max_length", 1000, CVar.REPLICATED | CVar.SERVER);

    #region Supermatter

    /// <summary>
    ///     With completely default supermatter values, Singuloose delamination will occur if engineers inject at least 900 moles of coolant per tile
    ///     in the crystal chamber. For reference, a gas canister contains 1800 moles of air. This Cvar directly multiplies the amount of moles required to singuloose.
    /// </summary>
    public static readonly CVarDef<float> SupermatterSingulooseMolesModifier =
        CVarDef.Create("supermatter.singuloose_moles_modifier", 1f, CVar.SERVER);

    /// <summary>
    ///     Toggles whether or not Singuloose delaminations can occur. If both Singuloose and Tesloose are disabled, it will always delam into a Nuke.
    /// </summary>
    public static readonly CVarDef<bool> SupermatterDoSingulooseDelam =
        CVarDef.Create("supermatter.do_singuloose", true, CVar.SERVER);

    /// <summary>
    ///     By default, Supermatter will "Tesloose" if the conditions for Singuloose are not met, and the core's power is at least 4000.
    ///     The actual reasons for being at least this amount vary by how the core was screwed up, but traditionally it's caused by "The core is on fire".
    ///     This Cvar multiplies said power threshold for the purpose of determining if the delam is a Tesloose.
    /// </summary>
    public static readonly CVarDef<float> SupermatterTesloosePowerModifier =
        CVarDef.Create("supermatter.tesloose_power_modifier", 1f, CVar.SERVER);

    /// <summary>
    ///     Toggles whether or not Tesloose delaminations can occur. If both Singuloose and Tesloose are disabled, it will always delam into a Nuke.
    /// </summary>
    public static readonly CVarDef<bool> SupermatterDoTeslooseDelam =
        CVarDef.Create("supermatter.do_tesloose", true, CVar.SERVER);

    /// <summary>
    ///     The cutoff on power properly doing damage, pulling shit around, and delaminating into a tesla.
    ///     The supermatter will also spawn anomalies, and gains +2 bolts of electricity.
    /// </summary>
    public static readonly CVarDef<float> SupermatterPowerPenaltyThreshold =
        CVarDef.Create("supermatter.power_penalty_threshold", 5000f, CVar.SERVER);

    /// <summary>
    ///     Above this, the supermatter spawns anomalies at an increased rate, and gains +1 bolt of electricity.
    /// </summary>
    public static readonly CVarDef<float> SupermatterSeverePowerPenaltyThreshold =
        CVarDef.Create("supermatter.power_penalty_threshold_severe", 7000f, CVar.SERVER);

    /// <summary>
    ///     Above this, the supermatter spawns pyro anomalies at an increased rate, and gains +1 bolt of electricity.
    /// </summary>
    public static readonly CVarDef<float> SupermatterCriticalPowerPenaltyThreshold =
        CVarDef.Create("supermatter.power_penalty_threshold_critical", 9000f, CVar.SERVER);

    /// <summary>
    ///     The minimum pressure for a pure ammonia atmosphere to begin being consumed.
    /// </summary>
    public static readonly CVarDef<float> SupermatterAmmoniaConsumptionPressure =
        CVarDef.Create("supermatter.ammonia_consumption_pressure", Atmospherics.OneAtmosphere * 0.01f, CVar.SERVER);

    /// <summary>
    ///     How the amount of ammonia consumed per tick scales with partial pressure.
    /// </summary>
    public static readonly CVarDef<float> SupermatterAmmoniaPressureScaling =
        CVarDef.Create("supermatter.ammonia_pressure_scaling", Atmospherics.OneAtmosphere * 0.05f, CVar.SERVER);

    /// <summary>
    ///     How much the amount of ammonia consumed per tick scales with the gas mix power ratio.
    /// </summary>
    public static readonly CVarDef<float> SupermatterAmmoniaGasMixScaling =
        CVarDef.Create("supermatter.ammonia_gas_mix_scaling", 0.3f, CVar.SERVER);

    /// <summary>
    ///     The amount of matter power generated for every mole of ammonia consumed.
    /// </summary>
    public static readonly CVarDef<float> SupermatterAmmoniaPowerGain =
        CVarDef.Create("supermatter.ammonia_power_gain", 10f, CVar.SERVER);

    /// <summary>
    ///     When true, bypass the normal checks to determine delam type, and instead use the type chosen by supermatter.forced_delam_type
    /// </summary>
    public static readonly CVarDef<bool> SupermatterDoForceDelam =
        CVarDef.Create("supermatter.do_force_delam", false, CVar.SERVER);

    /// <summary>
    ///     If supermatter.do_force_delam is true, this determines the delamination type, bypassing the normal checks.
    /// </summary>
    public static readonly CVarDef<DelamType> SupermatterForcedDelamType =
        CVarDef.Create("supermatter.forced_delam_type", DelamType.Singulo, CVar.SERVER);

    /// <summary>
    ///     Maximum safe operational temperature in degrees Celsius.
    ///     Supermatter begins taking damage above this temperature.
    /// </summary>
    public static readonly CVarDef<float> SupermatterHeatPenaltyThreshold =
        CVarDef.Create("supermatter.heat_penalty_threshold", 40f, CVar.SERVER);

    /// <summary>
    ///     The percentage of the supermatter's matter power that is converted into power each atmos tick.
    /// </summary>
    public static readonly CVarDef<float> SupermatterMatterPowerConversion =
        CVarDef.Create("supermatter.matter_power_conversion", 10f, CVar.SERVER);

    /// <summary>
    ///     Divisor on the amount of damage that the supermatter takes from absorbing hot gas.
    /// </summary>
    public static readonly CVarDef<float> SupermatterMoleHeatPenalty =
        CVarDef.Create("supermatter.mole_heat_penalty", 350f, CVar.SERVER);

    /// <summary>
    ///     Above this threshold the supermatter will delaminate into a singulo and take damage from gas moles.
    ///     Below this threshold, the supermatter can heal damage.
    /// </summary>
    public static readonly CVarDef<float> SupermatterMolePenaltyThreshold =
        CVarDef.Create("supermatter.mole_penalty_threshold", 1800f, CVar.SERVER);

    /// <summary>
    ///     Divisor on the amount of oxygen released during atmospheric reactions.
    /// </summary>
    public static readonly CVarDef<float> SupermatterOxygenReleaseModifier =
        CVarDef.Create("supermatter.oxygen_release_modifier", 325f, CVar.SERVER);

    /// <summary>
    ///     Divisor on the amount of plasma released during atmospheric reactions.
    /// </summary>
    public static readonly CVarDef<float> SupermatterPlasmaReleaseModifier =
        CVarDef.Create("supermatter.plasma_release_modifier", 750f, CVar.SERVER);

    /// <summary>
    ///     Percentage of inhibitor gas needed before the charge inertia chain reaction effect starts.
    /// </summary>
    public static readonly CVarDef<float> SupermatterPowerlossInhibitionGasThreshold =
        CVarDef.Create("supermatter.powerloss_inhibition_gas_threshold", 0.2f, CVar.SERVER);

    /// <summary>
    ///     Moles of the gas needed before the charge inertia chain reaction effect starts.
    ///     Scales powerloss inhibition down until this amount of moles is reached.
    /// </summary>
    public static readonly CVarDef<float> SupermatterPowerlossInhibitionMoleThreshold =
        CVarDef.Create("supermatter.powerloss_inhibition_mole_threshold", 20f, CVar.SERVER);

    /// <summary>
    ///     Bonus powerloss inhibition boost if this amount of moles is reached.
    /// </summary>
    public static readonly CVarDef<float> SupermatterPowerlossInhibitionMoleBoostThreshold =
        CVarDef.Create("supermatter.powerloss_inhibition_mole_boost_threshold", 500f, CVar.SERVER);

    /// <summary>
    ///     Base amount of radiation that the supermatter emits.
    /// </summary>
    public static readonly CVarDef<float> SupermatterRadsBase =
        CVarDef.Create("supermatter.rads_base", 4f, CVar.SERVER);

    /// <summary>
    ///     Directly multiplies the amount of rads put out by the supermatter. Be VERY conservative with this.
    /// </summary>
    public static readonly CVarDef<float> SupermatterRadsModifier =
        CVarDef.Create("supermatter.rads_modifier", 1f, CVar.SERVER);

    /// <summary>
    ///     Multiplier on the overall power produced during supermatter atmospheric reactions.
    /// </summary>
    public static readonly CVarDef<float> SupermatterReactionPowerModifier =
        CVarDef.Create("supermatter.reaction_power_modifier", 0.55f, CVar.SERVER);

    /// <summary>
    ///     Divisor on the amount that atmospheric reactions increase the supermatter's temperature.
    /// </summary>
    public static readonly CVarDef<float> SupermatterThermalReleaseModifier =
        CVarDef.Create("supermatter.thermal_release_modifier", 5f, CVar.SERVER);

    /// <summary>
    ///     How often the supermatter should announce its status.
    /// </summary>
    public static readonly CVarDef<float> SupermatterYellTimer =
        CVarDef.Create("supermatter.yell_timer", 60f, CVar.SERVER);

    #endregion
}
