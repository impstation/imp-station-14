using Content.Shared.Heretic.EntitySystems;

namespace Content.Shared.Heretic.Components;

[RegisterComponent, Access(typeof(HereticSoulFragmentSystem))]
public sealed partial class HereticSoulFragmentComponent : Component
{
    [DataField]
    public LocId Message { get; private set; } = string.Empty;
}
