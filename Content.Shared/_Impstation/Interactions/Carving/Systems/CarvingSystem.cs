using Content.Shared._Impstation.Interactions.Carving.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;

namespace Content.Shared._Impstation.Interactions.Carving.Systems;

public sealed partial class CarvingSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        //SubscribeLocalEvent<CarvableComponent, InteractUsingEvent>(TryCarve);

        Subs.BuiEvents<CarvableComponent>(CarvingUiKey.Key, subs =>
        {
            subs.Event<CarvedEvent>(Personalise);
        });
    }

    private void Personalise(Entity<CarvableComponent> ent, ref CarvedEvent args)
    {
        if (args.Sprite == null)
            return;

        RaiseNetworkEvent
            (
            new CarveSpriteEvent
                (
                GetNetEntity(ent.Owner),
                args.Sprite,
                args.CarveName
                )
            );
    }

    private bool TryCarve(Entity<CarvableComponent> ent, EntityUid item)
    {
        if (ent.Comp.Carved)
            return true;

        if (!TryComp<ToolComponent>(item, out var tool) ||
            !_toolSystem.HasQuality(item, ent.Comp.Tool, tool))
            return true;



        return true;
    }
}
