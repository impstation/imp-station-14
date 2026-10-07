using Content.Server._Impstation.Mind.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs;

namespace Content.Server._Impstation.Mind;

/// <summary>
/// This handles transfering a target's mind to their killer when they die. 
/// made for monarch rays but i ended up doing something different
/// </summary>
public sealed class TransferMindToKillerSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TransferMindToKillerComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(Entity<TransferMindToKillerComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            if (!_mindSystem.TryGetMind(ent, out var mindId, out var mind))
                return;

            if (args.Origin == null)
                return;

            var transferMind = args.Origin;
            _mindSystem.TransferTo(mindId, transferMind, mind: mind);
        }
    }
}
