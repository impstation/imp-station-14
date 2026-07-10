using Content.Shared.Gravity;
using Content.Shared._Impstation.StatusEffectNew.Components;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._Impstation.StatusEffectsNew;

/// <summary>
///     Makes the target float for a period of time
/// </summary>

public sealed class FloatingSystem : EntitySystem
{
    [Dependency] private readonly SharedGravitySystem _gravity = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<FloatingStatusEffectComponent, StatusEffectRelayedEvent<IsWeightlessEvent>>(OnIsWeightless);
        SubscribeLocalEvent<FloatingStatusEffectComponent, StatusEffectAppliedEvent>(OnApplied);
        SubscribeLocalEvent<FloatingStatusEffectComponent, StatusEffectRemovedEvent>(OnRemoved);
    }
    private void OnIsWeightless(Entity<FloatingStatusEffectComponent> ent, ref StatusEffectRelayedEvent<IsWeightlessEvent> args)
    {
        if (args.Args.Handled)
            return;

        var ev = args.Args;
        ev.Handled = true;
        ev.IsWeightless = true;
        args.Args = ev;
    }
    private void OnApplied(Entity<FloatingStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        _gravity.RefreshWeightless(args.Target, true);
    }
    private void OnRemoved(Entity<FloatingStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        _gravity.RefreshWeightless(args.Target, false);
    }
}
