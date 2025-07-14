using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Overlays;
using Robust.Shared.Containers;

namespace Content.Server.Implants;

public sealed class MedHUDImplantSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MedHUDImplantComponent, ImplantImplantedEvent>(OnImplantImplanted);
        SubscribeLocalEvent<MedHUDImplantComponent, EntGotRemovedFromContainerMessage>(OnRemove);
    }

    /// <summary>
    /// If implanted with a medHUD implant, installs the necessary intrinsic medHUD components
    /// </summary>
    private void OnImplantImplanted(Entity<MedHUDImplantComponent> ent, ref ImplantImplantedEvent args)
    {
        if (args.Implanted == null)
            return;

        EnsureComp<ShowHealthBarsComponent>(args.Implanted.Value);
        EnsureComp<ShowHealthIconsComponent>(args.Implanted.Value);
    }

    /// <summary>
    /// Removes intrinsic medHUD components once the MedHUD implant is removed
    /// </summary>
    private void OnRemove(Entity<MedHUDImplantComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (TryComp<ShowHealthBarsComponent>(args.Container.Owner, out var showHealthBarsComponent))
        {
            RemCompDeferred<ShowHealthBarsComponent>(args.Container.Owner);
        }
        if (TryComp<ShowHealthIconsComponent>(args.Container.Owner, out var showHealthIconsComponent))
        {
            RemCompDeferred<ShowHealthIconsComponent>(args.Container.Owner);
        }
    }
}