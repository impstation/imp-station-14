using Content.Shared._Impstation.Slasher.Components;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client._Impstation.Slasher;

/// <summary>
/// Adds the soul-harvested status icon to victims that currently have a harvest lockout marker.
/// </summary>
public sealed class SlasherSoulHarvestedSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    /// <summary>
    /// Subscribes local events and prepares dependencies for this system.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlasherSoulHarvestedComponent, GetStatusIconsEvent>(OnGetStatusIcon);
    }

    /// <summary>
    /// Type definition for OnGetStatusIcon.
    /// </summary>
    /// <param name="ent">Entity tuple containing UID and component data.</param>
    /// <param name="args">Event arguments for this callback.</param>
    private void OnGetStatusIcon(Entity<SlasherSoulHarvestedComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_prototype.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
}
