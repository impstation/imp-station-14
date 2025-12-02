using Content.Shared.Heretic.Components;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Heretic;

public sealed class HellVictimSystem : Shared.Heretic.EntitySystems.SharedHellVictimSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HellVictimComponent, GetStatusIconsEvent>(GetSacIcon);
    }

    private void GetSacIcon(Entity<HellVictimComponent> ent, ref GetStatusIconsEvent args)
    {
        var iconPrototype = _prototype.Index(ent.Comp.StatusIcon);
        args.StatusIcons.Add(iconPrototype);
    }
}
