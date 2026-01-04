using Content.Shared.Actions;
using Content.Shared.Body.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared.Destroy;

//<summary>
// System for the Destroyer component, which allows an entity to "destroy" other entities acording to a whitelist.
//</summary>
public sealed class DestroySystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DestroyerComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<DestroyerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<DestroyerComponent, DestroyActionEvent>(OnDestroyAction);
        SubscribeLocalEvent<DestroyerComponent, DestroyDoAfterEvent>(OnDoAfter);
    }

    private void OnInit(Entity<DestroyerComponent> ent, ref MapInitEvent args)
    {
        _actionsSystem.AddAction(ent.Owner, ref ent.Comp.DestroyActionEntity, ent.Comp.DestroyAction);
    }

    private void OnShutdown(Entity<DestroyerComponent> ent, ref ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(ent.Owner, ent.Comp.DestroyActionEntity);
    }

    /// <summary>
    /// The Destroy action
    /// </summary>
    private void OnDestroyAction(Entity<DestroyerComponent> ent, ref DestroyActionEvent args)
    {
        if (args.Handled || _whitelistSystem.IsWhitelistFailOrNull(ent.Comp.Whitelist, args.Target))
            return;

        args.Handled = true;
        var target = args.Target;

        _popupSystem.PopupClient(Loc.GetString("Destroy-action-popup-message-structure"), ent.Owner, ent.Owner);

        if (ent.Comp.SoundStructureDestroy != null)
            _audioSystem.PlayPredicted(ent.Comp.SoundStructureDestroy, ent.Owner, ent.Owner, ent.Comp.SoundStructureDestroy.Params);

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, ent.Owner, ent.Comp.StructureDestroyTime, new DestroyDoAfterEvent(), ent.Owner, target: target, used: ent.Owner)
        {
            BreakOnMove = true,
        });
    }

    private void OnDoAfter(Entity<DestroyerComponent> ent, ref DestroyDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        //TODO: Figure out a better way of removing structures via Destroy that still entails standing still and waiting for a DoAfter. Somehow.
        //If it's not alive, it must be a structure.
        // Delete if the thing isn't in the stomach storage whitelist (or the stomach whitelist is null/empty)
        else if (args.Args.Target != null)
        {
            PredictedQueueDel(args.Args.Target.Value);
        }

        _audioSystem.PlayPredicted(ent.Comp.SoundDestroy, ent.Owner, ent.Owner);
    }
}

public sealed partial class DestroyActionEvent : EntityTargetActionEvent;

[Serializable, NetSerializable]
public sealed partial class DestroyDoAfterEvent : SimpleDoAfterEvent;

