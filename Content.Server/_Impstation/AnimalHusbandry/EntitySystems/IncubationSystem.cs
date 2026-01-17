using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared._Impstation.AnimalHusbandry.Components;
using Content.Shared.Interaction;
using Content.Shared.Power;

namespace Content.Server._Impstation.AnimalHusbandry.EntitySystems;
public sealed class IncubationSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IncubationComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<IncubationComponent, IncubatingAttemptEvent>(Incubate);
        SubscribeLocalEvent<IncubationComponent, IncubationDoAfterEvent>(OnDoAfter);
    }

    private void OnAfterInteract(Entity<IncubationComponent> entity, ref AfterInteractEvent args)
    {
        SetAppearance(entity, IncubatorStatus.Active);
    }

    private void OnDoAfter(Entity<IncubationComponent> entity, ref IncubationDoAfterEvent args)
    {
        SetAppearance(entity, IncubatorStatus.Inactive);
    }

    private void Incubate(Entity<IncubationComponent> entity, ref IncubatingAttemptEvent args)
    {
    }

    public void SetAppearance(EntityUid uid, IncubatorStatus state, AppearanceComponent? appearanceComponent = null)
    {
        if (!Resolve(uid, ref appearanceComponent, false))
            return;
        _appearance.SetData(uid, IncubatorVisualizerLayers.Status, state, appearanceComponent);
    }
}
