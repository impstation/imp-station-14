using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Robust.Shared.Reflection;

namespace Content.Shared._Impstation.AnimalHusbandry.BreedEffects;
/// <summary>
/// Base of all Breed Effects
/// These will be run when a Mob breeds or gives birth
/// </summary>
[DataDefinition]
public abstract partial class BaseBreedEffect
{
    [DataField("activeOnBreed")]
    public bool ApplyOnBreed = false;

    [DataField("activeOnBirth")]
    public bool ApplyOnBirth = false;

    public virtual void BreedEffect(EntityUid self, EntityUid partner, IEntityManager? entitySystem = null) { }
    public virtual void BirthEffect(EntityUid self, EntityUid partner, IEntityManager? entitySystem = null) { }
}
