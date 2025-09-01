using Content.Server.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles; // imp
using Content.Shared._DV.Roles; // imp
using Content.Shared.Roles.Jobs; // imp
using Content.Server.GameTicking.Rules;
using Content.Server.Revolutionary.Components;
using Content.Server.Roles; // imp
using Robust.Shared.Player; // imp
using Robust.Shared.Random;
using System.Linq;
using Content.Server._Goobstation.Roles; // imp
using Content.Shared.Roles.Components; // imp

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles assinging a target to an objective entity with <see cref="TargetObjectiveComponent"/> using different components.
/// These can be combined with condition components for objective completions in order to create a variety of objectives.
/// </summary>
public sealed class PickObjectiveTargetSystem : EntitySystem
{
    [Dependency] private readonly TargetObjectiveSystem _target = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!; // imp edit
    [Dependency] private readonly TraitorRuleSystem _traitorRule = default!;
    [Dependency] private readonly SharedJobSystem _job = default!; // imp edit
    [Dependency] private readonly SharedRoleSystem _role = default!; // imp edit

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PickSpecificPersonComponent, ObjectiveAssignedEvent>(OnSpecificPersonAssigned);
        SubscribeLocalEvent<PickRandomPersonComponent, ObjectiveAssignedEvent>(OnRandomPersonAssigned);

        SubscribeLocalEvent<PickRandomAntagComponent, ObjectiveAssignedEvent>(OnAntagAssigned); // imp
    }

    private void OnSpecificPersonAssigned(Entity<PickSpecificPersonComponent> ent, ref ObjectiveAssignedEvent args)
    {
        // invalid objective prototype
        if (!TryComp<TargetObjectiveComponent>(ent.Owner, out var target))
        {
            args.Cancelled = true;
            return;
        }

        // target already assigned
        if (target.Target != null)
            return;

        if (args.Mind.OwnedEntity == null)
        {
            args.Cancelled = true;
            return;
        }

        var user = args.Mind.OwnedEntity.Value;
        if (!TryComp<TargetOverrideComponent>(user, out var targetComp) || targetComp.Target == null)
        {
            args.Cancelled = true;
            return;
        }

        _target.SetTarget(ent.Owner, targetComp.Target.Value);
    }

    private void OnRandomPersonAssigned(Entity<PickRandomPersonComponent> ent, ref ObjectiveAssignedEvent args)
    {
        // invalid objective prototype
        if (!TryComp<TargetObjectiveComponent>(ent, out var target))
        {
            args.Cancelled = true;
            return;
        }

        // target already assigned
        if (target.Target != null)
            return;

        // couldn't find a target :(
        if (_mind.PickFromPool(ent.Comp.Pool, ent.Comp.Filters, args.MindId) is not {} picked)
        {
            args.Cancelled = true;
            return;
        }

        _target.SetTarget(ent, picked, target);
    }

    // imp addition for Bounty Hunters
    private void OnAntagAssigned(Entity<PickRandomAntagComponent> ent, ref ObjectiveAssignedEvent args)
    {
        // invalid prototype
        if (!TryComp<TargetObjectiveComponent>(ent.Owner, out var target))
        {
            args.Cancelled = true;
            return;
        }

        var antags = new HashSet<EntityUid>();
        var allHumans = _mind.GetAliveHumans(args.MindId); //imp edit - just get the mind ID

        //check for any fugitives, and target them if they're present
        foreach (var person in allHumans)
        {
            // imp edit
            var mindId = person.Owner;
            var mind = person.Comp;

            // get the mind and its owned entity
            if (mind.OwnedEntity is not { } owned)
            {
                continue;
            }

            //is it a fugitive?
            if (_role.MindHasRole<FugitiveRoleComponent>(mindId))
            {
                antags.Add(person);
                continue;
            }
            // imp edit end
        }

        // if there're no fugitives, check for any of the other valid antagonist mindroles
        if (antags.Count == 0)
        {
            foreach (var person in allHumans)
            {
                // imp edit
                var mindId = person.Owner;
                var mind = person.Comp;

                // get the mind and its owned entity
                if (mind.OwnedEntity is not { } owned)
                {
                    continue;
                }

                //huge list of every single whitelisted antag's role component
                if (_role.MindHasRole<GoobChangelingRoleComponent>(mindId)  /*Changeling*/
                /*|| _role.MindHasRole<RevolutionaryRoleComponent>(mindId)/*Head Rev (REVS USE THE SAME MINDROLE WHYYY)*/
                || _role.MindHasRole<HereticRoleComponent>(mindId)      /*Heretic*/
                || _role.MindHasRole<ThiefRoleComponent>(mindId)        /*Thief*/
                || _role.MindHasRole<TraitorRoleComponent>(mindId)      /*Traitor*/

                || _role.MindHasRole<BountyHunterRoleComponent>(mindId) /*Fellow Bunters*/
                || _role.MindHasRole<NinjaRoleComponent>(mindId)       /*Ninja*/
                || _role.MindHasRole<NukeopsRoleComponent>(mindId)     /*Nukies*/
                || _role.MindHasRole<ParadoxCloneRoleComponent>(mindId)/*Paradox Clone*/
                || _role.MindHasRole<SynthesisRoleComponent>(mindId)   /*Synthesis Specialist*/
                || _role.MindHasRole<WizardRoleComponent>(mindId)      /*Wizard*/
                )
                {
                    antags.Add(person);
                    continue;
                }
                // imp edit end
            }
        }

        // failed to roll an antag as a target
            if (antags.Count == 0)
            {
                //fallback to target a random head
                foreach (var person in allHumans)
                {
                    if (TryComp<MindComponent>(person, out var mind) && mind.OwnedEntity is { } owned && HasComp<CommandStaffComponent>(owned))
                        antags.Add(person);
                }

                // just go for some random person if there's no command.
                if (antags.Count == 0)
                {
                    antags = new HashSet<EntityUid>(allHumans.Select(p => p.Owner)); //imp
                }

                // One last check for the road, then cancel it if there's nothing left
                if (antags.Count == 0)
                {
                    args.Cancelled = true;
                    return;
                }
            }
        var randomTarget = _random.Pick(antags);
        _target.SetTarget(ent.Owner, randomTarget, target);
    }
    // imp edit end
}
