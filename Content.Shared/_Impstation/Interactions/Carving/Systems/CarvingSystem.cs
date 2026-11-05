using Content.Shared._Impstation.CCVar;
using Content.Shared._Impstation.Interactions.Carving.Components;
using Content.Shared._Impstation.Interactions.Carving.DoAfters;
using Content.Shared._Impstation.Interactions.Carving.Events;
using Content.Shared.DoAfter;
using Content.Shared.EntityEffects;
using Content.Shared.Interaction;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using System.ComponentModel;

namespace Content.Shared._Impstation.Interactions.Carving.Systems;

public sealed partial class CarvingSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effectsSystem = default!;

    private int _nameMaxLength = 32;
    private int _descMaxLength = 256;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CarvableComponent, PersonaliseDoAfterEvent>(OnDoAfter);

        Subs.BuiEvents<CarvableComponent>(CarvingUiKey.Key, subs =>
        {
            subs.Event<CarvedEvent>(Personalise);
        });
    }

    private void Personalise(Entity<CarvableComponent> ent, ref CarvedEvent args)
    {
        if (args.Sprite == null ||
            !IsEntityTextValid(ent, args.CarveName, ""))
            return;

        // Cancel if one is in progress
        if (ent.Comp.DoAfter != null)
        {
            _doAfterSystem.Cancel(ent.Comp.DoAfter);
            ent.Comp.DoAfter = null;
        }

        var doAfter = new PersonaliseDoAfterEvent
            (
            GetNetEntity(ent.Owner),
            args.Sprite,
            args.CarveName
            );

        var doAfterCancelled = !_doAfterSystem.TryStartDoAfter
            (
            new DoAfterArgs
                (
                EntityManager,
                args.Actor,
                ent.Comp.CarveDelay,
                doAfter,
                ent,
                target: ent.Owner,
                used: ent.Owner
                )
            {
                NeedHand = false,
                BreakOnMove = true,
                BreakOnDamage = true,
                BreakOnHandChange = true
            },
            out var doAfterId);

        ent.Comp.DoAfter = doAfterId;
    }

    private void OnDoAfter(Entity<CarvableComponent> ent, ref PersonaliseDoAfterEvent args)
    {
        ent.Comp.DoAfter = null;
        if (args.Handled || args.Target == null || args.Cancelled)
            return;

        UpdateMeta(ent, args.CarveName, "");

        RaiseNetworkEvent
            (
            new CarveSpriteEvent
                (
                GetNetEntity(ent.Owner),
                args.Sprite,
                args.CarveName
                )
            );

        _effectsSystem.ApplyEffects(ent, ent.Comp.Effects.ToArray(), user: ent);
    }

    private bool TryCarve(Entity<CarvableComponent> ent, EntityUid item)
    {
        if (ent.Comp.Carved)
            return true;

        if (!TryComp<ToolComponent>(item, out var tool) ||
            !_toolSystem.HasQuality(item, ent.Comp.Tool, tool))
            return true;



        return true;
    }

    public void AddLayerToSprite(Entity<CarvableComponent> entity, string stateExtension, Enum mapping, bool addVisible = false)
    {
        RaiseNetworkEvent
            (
            new AddPersonalisationState
                (
                GetNetEntity(entity),
                stateExtension,
                mapping,
                addVisible
                )
            );
    }
}
