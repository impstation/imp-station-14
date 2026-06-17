using Content.Server._Impstation.Administration.Components;
using Content.Shared._Impstation.Administration.Components;
using Content.Shared.Nutrition.Components;

namespace Content.Server._Impstation.Administration.Systems;

public sealed class GetSignSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FoodSequenceStartPointComponent, GetSignAddedEvent>(OnAdd);
    }

    private void OnAdd(Entity<FoodSequenceStartPointComponent> ent, ref GetSignAddedEvent args)
    {
        EnsureComp<GetSignComponent>(ent);
    }
}
