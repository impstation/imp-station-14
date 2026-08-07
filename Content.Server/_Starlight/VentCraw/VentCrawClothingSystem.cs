// Initial file ported from the Starlight project repo, located at https://github.com/ss14Starlight/space-station-14

using Content.Shared.Clothing;
using Content.Shared._Starlight.VentCraw.Components;
using Content.Shared._Starlight.VentCraw;

namespace Content.Server._Starlight.VentCraw;

public sealed class VentCrawClothingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VentCrawClothingComponent, ClothingGotEquippedEvent>(OnClothingEquip);
        SubscribeLocalEvent<VentCrawClothingComponent, ClothingGotUnequippedEvent>(OnClothingUnequip);
    }

    private void OnClothingEquip(Entity<VentCrawClothingComponent> ent, ref ClothingGotEquippedEvent args)
    {
        AddComp<VentCrawlerComponent>(args.Wearer);
    }

    private void OnClothingUnequip(Entity<VentCrawClothingComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        RemComp<VentCrawlerComponent>(args.Wearer);
    }
}
