using Content.Shared._Impstation.Heretic.Components;
using Content.Shared._Impstation.Heretic.EntitySystems;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client._Impstation.Heretic.EntitySystems;

public sealed class HellVictimSystem : SharedHellVictimSystem
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
