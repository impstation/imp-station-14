using Content.Shared._Impstation.CosmicCult.Components;
using Content.Shared._Impstation.CosmicCult;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client._Impstation.CosmicCult;

public sealed class CosmicCultSystem : SharedCosmicCultSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CosmicCultComponent, GetStatusIconsEvent>(GetCosmicCultIcon);
        SubscribeLocalEvent<CosmicCultLeadComponent, GetStatusIconsEvent>(GetCosmicCultLeadIcon);
    }

    private void GetCosmicCultIcon(Entity<CosmicCultComponent> ent, ref GetStatusIconsEvent args)
    {
        if (HasComp<CosmicCultLeadComponent>(ent))
            return;

        if (_prototype.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }

    private void GetCosmicCultLeadIcon(Entity<CosmicCultLeadComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_prototype.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
}
