using Content.Server.Heretic.EntitySystems;

namespace Content.Server.Heretic.Components;

[RegisterComponent, Access(typeof(EldritchItemSystem))]
public sealed partial class EldritchItemComponent : Component
{
    [DataField] public bool Spent = false;
}
