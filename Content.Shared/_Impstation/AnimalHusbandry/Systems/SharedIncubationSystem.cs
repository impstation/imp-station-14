using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared._Impstation.AnimalHusbandry.Components;
using Content.Shared.Interaction;
using Robust.Shared.Containers;

namespace Content.Shared._Impstation.AnimalHusbandry.Systems;
/// <summary>
/// "Why does this exist rather than just putting this stuff into the IncubationSystem.cs server file like the Microwave does?"
/// Phenomenal question. It breaks when I do that.
/// This class purely exists to update the visuals.
/// </summary>
public abstract partial class SharedIncubationSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IncubatorComponent, InteractUsingEvent>(OnAfterInteract);
        SubscribeLocalEvent<IncubatorComponent, IncubationDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<IncubatorComponent, EntRemovedFromContainerMessage>(IncubatorEmpty);
    }

    private void OnAfterInteract(Entity<IncubatorComponent> entity, ref InteractUsingEvent args)
    {
        SetAppearance(entity, IncubatorStatus.Active);
    }

    private void OnDoAfter(Entity<IncubatorComponent> entity, ref IncubationDoAfterEvent args)
    {
        //SetAppearance(entity, IncubatorStatus.Inactive);
    }

    private void IncubatorEmpty(Entity<IncubatorComponent> entity, ref EntRemovedFromContainerMessage args)
    {
        SetAppearance(entity, IncubatorStatus.Inactive);
    }

    public void SetAppearance(EntityUid uid, IncubatorStatus state, AppearanceComponent? appearanceComponent = null)
    {
        if (!Resolve(uid, ref appearanceComponent, false))
            return;
        _appearance.SetData(uid, IncubatorVisualizerLayers.Status, state);
    }
}
