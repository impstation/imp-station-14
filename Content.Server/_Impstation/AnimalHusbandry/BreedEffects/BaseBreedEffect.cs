using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Robust.Shared.Reflection;

namespace Content.Server._Impstation.AnimalHusbandry.BreedEffects;
/// <summary>
/// Base of all Breed Effects
/// These will be run when a Mob breeds or gives birth
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class BaseBreedEffect
{
    [DataField("activeOnBreed")]
    public bool ApplyOnBreed = false;

    [DataField("activeOnBirth")]
    public bool ApplyOnBirth = false;

    protected static IEntityManager? _entManager;

    public virtual void InitializeBreedEffects() { _entManager ??= IoCManager.Resolve<IEntityManager>(); }

    /// <summary>
    /// To be called when an effect is added to a mob
    /// </summary>
    /// <param name="self"></param>
    public virtual void OnEffectAddition(EntityUid self) { _entManager ??= IoCManager.Resolve<IEntityManager>(); }

    /// <summary>
    /// Called when an entity gets bred with
    /// </summary>
    /// <param name="self"></param>
    /// <param name="partner"></param>
    /// <param name="entitySystem"></param>
    public virtual void BreedEffect(EntityUid self, EntityUid partner) { _entManager ??= IoCManager.Resolve<IEntityManager>(); }

    /// <summary>
    /// Called when an entity gives birth
    /// </summary>
    /// <param name="self"></param>
    /// <param name="offspring"></param>
    /// <param name="entitySystem"></param>
    public virtual void BirthEffect(EntityUid self, EntityUid offspring) { _entManager ??= IoCManager.Resolve<IEntityManager>(); }
}
