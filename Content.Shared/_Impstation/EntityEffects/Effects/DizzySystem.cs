using Content.Shared.Drunk;
using Content.Shared.Movement.Systems;

namespace Content.Shared._Impstation.EntityEffects.Effects;

/// <summary>
/// Handles the 'dizzy' status effect (reversing a user's controls).
/// </summary>
public sealed class DizzySystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedDrunkSystem _drunkSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DizzyComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<DizzyComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<DizzyComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
    }

    private void OnStartup(Entity<DizzyComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.Dizzy)
            return;
        ent.Comp.Dizzy = true;
        _movementSpeedModifier.RefreshMovementSpeedModifiers(ent);
        UpdateAppearance(ent.Owner);
    }

    private void OnShutdown(Entity<DizzyComponent> ent, ref ComponentShutdown args)
    {
        _drunkSystem.TryRemoveDrunkenessTime(ent, ent.Comp.StatusTime.TotalSeconds);
        ent.Comp.StatusTime = TimeSpan.Zero;

        ent.Comp.SprintSpeedModifier = 1f;
        ent.Comp.WalkSpeedModifier = 1f;

        _movementSpeedModifier.RefreshMovementSpeedModifiers(ent);
        UpdateAppearance(ent.Owner);
    }

    /// <summary>
    /// Applies the dizzy status effect to the specified entity.
    /// </summary>
    /// <param name="uid"> Entity to apply effect to </param>
    /// <param name="length"> Total time in seconds to apply effect for </param>
    public void MakeDizzy(EntityUid ent, float length)
    {
        var dizzy = EnsureComp<DizzyComponent>(ent);
        dizzy.TimeRemaining = length;
        dizzy.Dizzy = true;

        UpdateAppearance(ent);
    }

    public void UpdateAppearance(Entity<DizzyComponent?, AppearanceComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2))
            return;
        _appearance.SetData(ent, DizzyVisuals.Stars, ent.Comp1.Dizzy, ent.Comp2);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DizzyComponent>();
        while (query.MoveNext(out var uid, out var dizzy))
        {
            dizzy.TimeRemaining -= frameTime;

            if (dizzy.TimeRemaining > 0)
            {
                // Multiplying by 2 is arbitrary but works for this case, it just prevents the time from running out
                _drunkSystem.TryApplyDrunkenness(uid, (float)dizzy.UpdateInterval.TotalSeconds * 2, applySlur: false);
                // storing the drunk time so we can remove it independently from other effects additions
                dizzy.StatusTime += dizzy.UpdateInterval * 2;
                continue;
            }

            // after time runs out
            RemCompDeferred<DizzyComponent>(uid);
        }
    }

    private void OnRefreshMovespeed(EntityUid uid, DizzyComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(component.WalkSpeedModifier, component.SprintSpeedModifier);
    }
}
