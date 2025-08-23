using Content.Server.GameTicking.Rules;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.GameTicking;

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
        EnsureComp<PacifiedComponent>(ev.Mob);
    }
}
