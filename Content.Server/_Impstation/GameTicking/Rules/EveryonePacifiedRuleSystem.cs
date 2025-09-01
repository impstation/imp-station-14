using Content.Server.GameTicking.Rules;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;

namespace Content.Server._Impstation.GameTicking.Rules;

/// <summary>
///     Manages <see cref="EveryonePacifiedRuleComponent"/>
/// </summary>
public sealed class EveryonePacifiedRuleSystem : GameRuleSystem<EveryonePacifiedRuleComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnSpawn);
    }

    private void OnSpawn(PlayerSpawnCompleteEvent ev)
    {
        var query = EntityQueryEnumerator<EveryonePacifiedRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out _, out var rule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, rule))
                continue;
            EnsureComp<PacifiedComponent>(ev.Mob);
            break;
        }
    }
}
