using Content.Server._Impstation.CosmicCult.Components;
using Content.Server.Body.Components;
using Content.Server.Medical.Components;
using Content.Server.PowerCell;
using Content.Server.Radio.Components;
using Content.Server.Temperature.Components;
using Content.Server.Traits.Assorted;
using Content.Shared._Impstation.CosmicCult;
using Content.Shared._Impstation.CosmicCult.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.MedicalScanner;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server._Impstation.CosmicCult;

public sealed class CleanseDeconversionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<CleanseOnUseComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<CleanseOnUseComponent, CleanseOnDoAfterEvent>(OnDoAfter);
    }

    private void OnAfterInteract(Entity<CleanseOnUseComponent> uid, ref AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || !HasComp<MobStateComponent>(args.Target))
            return;
        Log.Debug($"Attempting to cleanse target's cult status!");

        var doAfterCancelled = !_doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, uid.Comp.ScanDelay, new CleanseOnDoAfterEvent(), uid, target: args.Target, used: uid)
        {
            BreakOnMove = true,
        });

        if (args.Target == args.User || doAfterCancelled )
            return;
    }

    private void OnDoAfter(Entity<CleanseOnUseComponent> uid, ref CleanseOnDoAfterEvent args)
    {
        Log.Debug($"Starting cleanse/deconversion!");
        if (_entMan.HasComponent<CosmicCultComponent>(args.Target))
        {
            DeconvertCultist(args.Target.Value);
        }

        args.Handled = true;
    }

    public void DeconvertCultist(EntityUid uid)
    {
        if (HasComp<CosmicCultComponent>(uid))
        {
            RemComp<CosmicCultComponent>(uid);
            RemComp<ActiveRadioComponent>(uid);
            RemComp<CleanseCorruptionComponent>(uid);
            RemComp<IntrinsicRadioReceiverComponent>(uid);
            RemComp<IntrinsicRadioTransmitterComponent>(uid);

            if (HasComp<CosmicCultLeadComponent>(uid))
                RemComp<CosmicCultLeadComponent>(uid);
        }
    }

}
