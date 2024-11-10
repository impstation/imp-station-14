using Content.Shared._Impstation.Cosmiccult;
using Content.Server._Impstation.Cosmiccult.Components;
using Content.Shared._Impstation.Cosmiccult.Components;
using Content.Shared.Mind;
using Content.Shared.Examine;

namespace Content.Server._Impstation.Cosmiccult;

public sealed class CosmicCultSystem : SharedCosmicCultSystem
{
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CosmicCultComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<CosmicItemComponent, ExaminedEvent>(OnCosmicItemExamine);
    }

    private void OnCompInit(Entity<CosmicCultComponent> ent, ref ComponentInit args)
    {
        // add monument visibility layer
        if (TryComp<EyeComponent>(ent, out var eye))
            _eye.SetVisibilityMask(ent, eye.VisibilityMask | CosmicMonumentComponent.LayerMask);
    }

    private void OnCosmicItemExamine(Entity<CosmicItemComponent> ent, ref ExaminedEvent args)
    {
        if (HasComp<CosmicCultComponent>(args.Examiner))
            return;

        args.PushMarkup(Loc.GetString("contraband-object-text-cosmiccult"));
    }
}
