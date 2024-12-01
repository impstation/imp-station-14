using Content.Server._Impstation.Cosmiccult.EntitySystems;

namespace Content.Server._Impstation.Cosmiccult.Components;

[RegisterComponent, Access(typeof(CosmicMonumentSystem))]
public sealed partial class CosmicMonumentComponent : Component
{
    [DataField] public bool Spent = false;


    [NonSerialized] public static int LayerMask = 777;
}
