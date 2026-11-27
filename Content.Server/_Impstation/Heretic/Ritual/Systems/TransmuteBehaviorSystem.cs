using Content.Server._Impstation.Heretic.Ritual;
using Content.Shared._Impstation.Heretic.Components;
using Content.Shared._Impstation.Heretic.Ritual;
using Content.Shared.Heretic.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Heretic.Ritual;

/// <summary>
/// Behavior for rituals which spawn an entity on success.
/// </summary>
[Serializable]
public sealed partial class TransmuteBehaviorSystem : SharedRitualBehaviorSystem
{
    public override void Initialize()
    {
        base.Initialize();
    }

    public void DoRitualEffect(EntityUid performer, EntityUid platform, TransmuteBehaviorComponent comp)
    {
        foreach (var ent in comp.OutputItems.Keys)
        {
            for (var i = 0; i < comp.OutputItems[ent]; i++)
            {
                var spawn = Spawn(ent, Transform(platform).Coordinates);

                // If it's a minion, make the performer the owner.
                if (TryComp<MinionComponent>(spawn, out var minion))
                {
                    minion.BoundOwner = performer;
                }
            }
        }
    }
}
