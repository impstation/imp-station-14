using Content.Server.GameTicking.Rules;
using Content.Shared._Impstation.Slasher.Components;
using Content.Shared.Flash;
using Content.Shared.Flash.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Revenant.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Server._Impstation.Slasher.Events;

/// <summary>
/// Applies a global haunting pulse to living crew: flash effect, haunted marker, and ambience audio.
/// </summary>
public sealed class SlasherGameRuleRevenantSpookSystem : SlasherPulseGameRuleSystem<SlasherGameRuleRevenantSpookComponent>
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedFlashSystem _flash = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    /// <summary>
    /// Finds eligible witnesses on the pulse station and applies the configured haunt effects.
    /// </summary>
    /// <param name="uid">Rule entity UID.</param>
    /// <param name="component">Rule configuration component.</param>
    /// <param name="gameRule">Base game-rule component.</param>
    /// <param name="args">Rule start event data.</param>
    protected override void Started(EntityUid uid, SlasherGameRuleRevenantSpookComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryGetPulseStation(out var chosenStation))
            return;

        var witnesses = new List<EntityUid>();
        var mobQuery = EntityQueryEnumerator<MobStateComponent, HumanoidAppearanceComponent>();
        while (mobQuery.MoveNext(out var mobUid, out _, out _))
        {
            if (!IsOnPulseStation(mobUid, chosenStation))
                continue;

            if (HasComp<RevenantComponent>(mobUid)
                || HasComp<SlasherRoleComponent>(mobUid)
                || HasComp<SlasherEffigyComponent>(mobUid)
                || !_mobState.IsAlive(mobUid))
                continue;

            witnesses.Add(mobUid);
        }

        if (witnesses.Count == 0)
            return;

        foreach (var witness in witnesses)
        {
            _flash.Flash(witness, null, null, component.FlashDuration, slowTo: 0.8f, displayPopup: false);
            EnsureComp<HauntedComponent>(witness);
        }

        _audio.PlayGlobal(component.HauntSound, Filter.Broadcast(), true);
    }
}
