using Content.Shared._Impstation.DamageBarrier;
using Content.Shared.Actions;
using Content.Shared.Humanoid;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._Impstation.Gastropoids.SnailShell;

public sealed partial class SnailShellSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDamageBarrierSystem _barrier = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SnailShellComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SnailShellComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SnailShellComponent, SnailShellActionEvent>(OnSnailShellAction);
        SubscribeLocalEvent<SnailShellComponent, DamageBarrierBreakEvent>(OnSnailShellBreak);
    }

    /// <summary>
    /// Gives the action to the entity
    /// </summary>
    private void OnStartup(Entity<SnailShellComponent> ent, ref ComponentStartup args)
    {
        _actions.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.Action);
    }

    /// <summary>
    /// Removes the action from the entity
    /// </summary>
    private void OnShutdown(Entity<SnailShellComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

    private void OnSnailShellAction(Entity<SnailShellComponent> ent, ref SnailShellActionEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (ent.Comp.Active)
        {
            RemCompDeferred<DamageBarrierComponent>(ent);
            SetShellVisibility(ent, false);
            ent.Comp.Active = false;
            Dirty(ent);
            return;
        }

        if (ent.Comp.Broken)
        {
            _popupSystem.PopupClient(Loc.GetString(ent.Comp.BrokenPopup), ent.Owner, ent.Owner);
            return;
        }

        _barrier.ApplyDamageBarrier(ent, ent.Comp.DamageModifier, null, ent.Comp.ShellHitSound, ent.Comp.ShellBreakSound);
        SetShellVisibility(ent, true);
        _audio.PlayPredicted(ent.Comp.ShellActivateSound, ent, ent);
        ent.Comp.Active = true;
        Dirty(ent);
    }

    private void OnSnailShellBreak(Entity<SnailShellComponent> ent, ref DamageBarrierBreakEvent args)
    {
        // ent.Comp.Broken = true;
        _popupSystem.PopupClient(Loc.GetString(ent.Comp.BreakPopup), ent.Owner, ent.Owner);
        var ev = new SnailShellBreakEvent();
        RaiseLocalEvent(ent, ref ev);
        // how tf are we healing it?
    }

    private void SetShellVisibility(Entity<SnailShellComponent, HumanoidAppearanceComponent?> ent, bool shellVisible)
    {
        if (!Resolve(ent, ref ent.Comp2))
            return;

        foreach (var layer in ent.Comp2.BaseLayers.Keys)
            foreach (var shellLayer in ent.Comp1.ShellLayers)
            {
                if (layer == shellLayer)
                    continue;
                _humanoid.SetLayerVisibility(ent.Owner, layer, !shellVisible);
            }


        Dirty(ent, ent.Comp2);
    }
}

/// <summary>
/// Relayed when the snail shell breaks while in use
/// </summary>
[ByRefEvent]
public record struct SnailShellBreakEvent;

/// <summary>
/// Relayed upon using the action
/// </summary>
public sealed partial class SnailShellActionEvent : InstantActionEvent { }
