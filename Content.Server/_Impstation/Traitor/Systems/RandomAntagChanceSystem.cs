using Content.Server._Impstation.Traitor.Components;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Roles;
using Content.Server.Zombies;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Impstation.Traitor.Systems;

/// <summary>
/// Makes entities with <see cref="RandomAntagChanceComponent"/> the defined antag at a set random chance.
/// </summary>
public sealed class RandomAntagChanceSystem : EntitySystem
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly RoleSystem _role = default!;
    [Dependency] private readonly ZombieSystem _zombie = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomAntagChanceComponent, MindAddedMessage>(OnMindAdded);
    }

    private void OnMindAdded(Entity<RandomAntagChanceComponent> ent, ref MindAddedMessage args)
    {
        if (!_player.TryGetSessionByEntity(ent, out var session))
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        if (random.Prob(ent.Comp.Chance))
        {
            DoRoles(session, args.Mind, ent.Comp.AntagRole);
        }
        else
        {
            DoRoles(session, args.Mind, ent.Comp.FallbackRole);
        }
        RemCompDeferred<RandomAntagChanceComponent>(ent);
    }

    private void DoRoles(ICommonSession session, Entity<MindComponent> ent, EntProtoId role)
    {
        // antag code is hell. woe to all ye who enter here
        // GOD I WISH THERE WAS A BETTER WAY TO DO THIS
        switch (role)
        {
            case "Traitor":
                _antag.ForceMakeAntag<TraitorRuleComponent>(session, role);
                return;
            case "InitialInfected":
                _antag.ForceMakeAntag<ZombieRuleComponent>(session, role);
                return;
            case "Zombie":
                _zombie.ZombifyEntity(ent);
                return;
            case "LoneOpsSpawn":
                _antag.ForceMakeAntag<NukeopsRuleComponent>(session, role);
                return;
            case "Revolutionary":
                _antag.ForceMakeAntag<RevolutionaryRuleComponent>(session, role);
                return;
            case "Thief":
                _antag.ForceMakeAntag<ThiefRuleComponent>(session, role);
                return;
            case "Changeling":
                _antag.ForceMakeAntag<ChangelingRuleComponent>(session, role);
                return;
            case "Heretic":
                _antag.ForceMakeAntag<HereticRuleComponent>(session, role);
                return;
            default:
                // i hate my life
                _role.MindAddRole(ent, role);
                return;
        }
    }
}
