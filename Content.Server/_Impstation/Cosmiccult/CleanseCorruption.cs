using System.Threading;
using Content.Server.Radio.Components;
using Content.Shared._Impstation.Cosmiccult.Components;
using Content.Shared.EntityEffects;
using Content.Shared.Jittering;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Impstation.Cosmiccult;

public sealed partial class CleanseCorruption : EntityEffect
{
    [Dependency] private readonly IGameTiming _timing = default!;

    [DataField]
    public float Amplitude = 5.0f;

    [DataField]
    public float Frequency = 20.0f;

    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(30.0f);
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Placeholder placeholdest"; //TODO: LANGUAGE & POLISH FOR CORRUPTION CLEASING
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        var entityManager = args.EntityManager;
        var uid = args.TargetEntity;
        if (!entityManager.TryGetComponent(uid, out CosmicCultComponent? cultEnt) ||
            cultEnt.DeconvertToken is not null)
        {
            return;
        }

        entityManager.System<SharedJitteringSystem>().DoJitter(uid, Time, true, Amplitude, Frequency);
        entityManager.EnsureComponent<CleanseCorruptionComponent>(uid, out var cleanse);

        cultEnt.DeconvertToken = new CancellationTokenSource();
        Robust.Shared.Timing.Timer.Spawn(Time, () => DeconvertCultist(uid, entityManager),
            cultEnt.DeconvertToken.Token);
    }
    private void DeconvertCultist(EntityUid uid, IEntityManager entityManager)
    {
        if (entityManager.HasComponent<CosmicCultComponent>(uid))
        {
            entityManager.RemoveComponent<CosmicCultComponent>(uid);
            entityManager.RemoveComponent<ActiveRadioComponent>(uid);
            entityManager.RemoveComponent<CleanseCorruptionComponent>(uid);
            entityManager.RemoveComponent<IntrinsicRadioReceiverComponent>(uid);
            entityManager.RemoveComponent<IntrinsicRadioTransmitterComponent>(uid);

            if (entityManager.HasComponent<CosmicCultLeadComponent>(uid))
                entityManager.RemoveComponent<CosmicCultLeadComponent>(uid);
        }
    }
}
