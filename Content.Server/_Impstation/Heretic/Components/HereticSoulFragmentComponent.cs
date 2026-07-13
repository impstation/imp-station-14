using Content.Server.Heretic.EntitySystems;

namespace Content.Server.Heretic.Components;

[RegisterComponent, Access(typeof(HereticSoulFragmentSystem))]
public sealed partial class HereticSoulFragmentComponent : Component
{
    [DataField]
    public LocId Message { get; private set; } = string.Empty;
}
