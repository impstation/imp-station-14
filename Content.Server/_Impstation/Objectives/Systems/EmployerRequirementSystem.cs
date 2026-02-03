using Content.Server._Impstation.Objectives.Components;
using Content.Shared._Impstation.TraitorFlavor;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;

namespace Content.Server._Impstation.Objectives.Systems;

public sealed class EmployerRequirementSystem : EntitySystem
{
    [Dependency] private readonly SharedRoleSystem _role = null!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmployerRequirementComponent, RequirementCheckEvent>(OnCheck);
    }

    private void OnCheck(Entity<EmployerRequirementComponent> requirement, ref RequirementCheckEvent args)
    {
        /*
        if (args.Cancelled)
            return;

        _role.MindHasRole<TraitorRoleComponent>(args.MindId, out var traitorRole);

        if (traitorRole?.Comp2.Employer == null)
            return;

        if (requirement.Comp.Blacklist != null)
        {
            if (requirement.Comp.Blacklist.Contains(traitorRole.Value.Comp2.Employer.ID))
            {
                args.Cancelled = true;
                return;
            }
        }

        if (requirement.Comp.Whitelist != null)
        {
            if (!requirement.Comp.Whitelist.Contains(traitorRole.Value.Comp2.Employer.ID))
            {
                args.Cancelled = true;
                return;
            }
        }
        */
    }
}
