using Content.Server.Emp;
using Content.Server.Power.Components;
using Content.Server.Power.Events;
using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;
using Content.Shared.Interaction;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Systems;

public sealed class ArtifactElectricityTriggerSystem : EntitySystem
{
    [Dependency] private readonly ArtifactSystem _artifactSystem = default!;
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ArtifactElectricityTriggerComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<ArtifactElectricityTriggerComponent, PowerPulseEvent>(OnPowerPulse);
        SubscribeLocalEvent<ArtifactElectricityTriggerComponent, EmpPulseEvent>(OnEmp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        List<Entity<ArtifactComponent>> toUpdate = new();
        var query = EntityQueryEnumerator<ArtifactElectricityTriggerComponent, PowerConsumerComponent, ArtifactComponent>();
        while (query.MoveNext(out var uid, out var trigger, out var power, out var artifact))
        {
            if (power.ReceivedPower <= trigger.MinPower)
                continue;

            toUpdate.Add((uid, artifact));
        }

        foreach (var a in toUpdate)
        {
            _artifactSystem.TryActivateArtifact(a, null, a);
        }
    }

    private void OnInteractUsing(Entity<ArtifactElectricityTriggerComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!_toolSystem.HasQuality(args.Used, SharedToolSystem.PulseQuality))
            return;

        args.Handled = _artifactSystem.TryActivateArtifact(ent, args.User);
    }

    private void OnPowerPulse(Entity<ArtifactElectricityTriggerComponent> ent, ref PowerPulseEvent args)
    {
        _artifactSystem.TryActivateArtifact(ent, args.User);
    }

    private void OnEmp(Entity<ArtifactElectricityTriggerComponent> ent, ref EmpPulseEvent args)
    {
        _artifactSystem.TryActivateArtifact(ent);
    }
}