using Content.Server._Impstation.Slasher.Components;
using Content.Server.Body.Systems;
using Content.Shared._Impstation.Slasher.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Robust.Shared.Physics.Events;

namespace Content.Server._Impstation.Slasher;

/// <summary>
/// Gibs humanoid mobs that contact the Slasher final boss.
/// Uses a sensor fixture so the boss phases through walls while still triggering on mob collision.
/// Pattern mirrors EventHorizonSystem.OnStartCollide.
/// </summary>
public sealed class SlasherFinalEntitySystem : EntitySystem
{
    [Dependency] private readonly BodySystem _body = default!;

    /// <summary>
    /// Subscribes local events and prepares dependencies for this system.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlasherFinalEntityComponent, StartCollideEvent>(OnStartCollide);
    }

    /// <summary>
    /// Type definition for OnStartCollide.
    /// </summary>
    /// <param name="ent">Entity tuple containing UID and component data.</param>
    /// <param name="args">Event arguments for this callback.</param>
    private void OnStartCollide(Entity<SlasherFinalEntityComponent> ent, ref StartCollideEvent args)
    {
        // Only trigger on the designated sensor fixture.
        if (args.OurFixtureId != ent.Comp.GibFixtureId)
            return;

        var other = args.OtherEntity;

        // Only gib humanoids — skips objects, projectiles, etc.
        if (!HasComp<HumanoidAppearanceComponent>(other))
            return;

        // Slashers are immune; don't gib each other or themselves.
        if (HasComp<SlasherRoleComponent>(other))
            return;

        // Don't gib corpses.
        if (TryComp<MobStateComponent>(other, out var mobState)
            && mobState.CurrentState == MobState.Dead)
            return;

        _body.GibBody(other, true);
    }
}
