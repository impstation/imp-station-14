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
    }

    private void TryCarve(Entity<CarvableComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryCarve(ent, args.Used);
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
