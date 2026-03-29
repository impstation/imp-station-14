using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Impstation.Genetics.Genes;

[Virtual]
public partial class BaseGeneEntitySystem : EntitySystem
{
    public virtual void OnGeneAdded(Entity<BaseGeneComponent> entity)
    {

    }

    public virtual void OnGeneRemoved(Entity<BaseGeneComponent> entity)
    {

    }
}
