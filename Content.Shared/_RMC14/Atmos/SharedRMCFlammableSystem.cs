using Content.Shared.Atmos.Components;

namespace Content.Shared._RMC14.Atmos;

public abstract class SharedRMCFlammableSystem : EntitySystem
{

    public override void Initialize()
    {
    }

    public bool IsOnFire(Entity<FlammableComponent?> ent)
    {
        return Resolve(ent, ref ent.Comp, false) && ent.Comp.OnFire;
    }

    public virtual void Extinguish(Entity<FlammableComponent?> flammable)
    {
    }

    public virtual void Pat(Entity<FlammableComponent?> flammable, int stacks)
    {
    }
}
