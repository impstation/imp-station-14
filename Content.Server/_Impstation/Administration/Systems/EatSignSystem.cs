using Content.Server._Impstation.Administration.Components;
using Content.Shared._Impstation.Administration.Components;
using Content.Shared.Nutrition.Components;

namespace Content.Server._Impstation.Administration.Systems;

public sealed class EatSignSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FoodSequenceStartPointComponent, EatSignAddedEvent>(OnAdd);
    }

    private void OnAdd(Entity<FoodSequenceStartPointComponent> ent, ref EatSignAddedEvent args)
    {
        EnsureComp<EatSignComponent>(ent);
    }
}
