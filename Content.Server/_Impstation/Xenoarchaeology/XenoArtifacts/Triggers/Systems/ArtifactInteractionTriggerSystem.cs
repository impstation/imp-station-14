using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;
using Content.Shared.Interaction;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Systems;

public sealed class ArtifactInteractionTriggerSystem : EntitySystem
{
    [Dependency] private readonly ArtifactSystem _artifactSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ArtifactInteractionTriggerComponent, PullStartedMessage>(OnPull);
        SubscribeLocalEvent<ArtifactInteractionTriggerComponent, AttackedEvent>(OnAttack);
        SubscribeLocalEvent<ArtifactInteractionTriggerComponent, InteractHandEvent>(OnInteract);
    }

    private void OnPull(Entity<ArtifactInteractionTriggerComponent> ent, ref PullStartedMessage args)
    {
        if (!ent.Comp.PullActivation)
            return;

        _artifactSystem.TryActivateArtifact(ent, args.PullerUid);
    }

    private void OnAttack(Entity<ArtifactInteractionTriggerComponent> ent, ref AttackedEvent args)
    {
        if (!ent.Comp.AttackActivation)
            return;

        _artifactSystem.TryActivateArtifact(ent, args.User);
    }

    private void OnInteract(Entity<ArtifactInteractionTriggerComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (!ent.Comp.EmptyHandActivation)
            return;

        args.Handled = _artifactSystem.TryActivateArtifact(ent, args.User);
    }
}