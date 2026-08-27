using Content.Shared._Impstation.Genetics.Components;
using Content.Shared.Explosion.EntitySystems;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Impstation.Genetics.Genes.Effects.Negative;

/// <summary>
/// Causes the chosen entity to explode when the effect activates
/// </summary>
public sealed partial class ExplodeGeneEffect : BaseGeneEffect
{
    private static SharedExplosionSystem? _explosionSystem;

    // What kind of explosion are we blasting?
    // For future readers, the types can be found in explosion.yml and are as follows:
    //     Default
    //     DemolitionCharge
    //     MicroBomb
    //     Radioactive
    //     Cryo
    //     PowerSink
    //     HardBomb
    //     FireBomb
    [DataField("explosionType")]
    private string _typeId = "Default";

    // How much power behind the blast?
    [DataField("intensity")]
    private float _intensity = 20f;

    // The lower this is the bigger the explosion radius
    [DataField("slope")]
    private float _slope = 10f;

    // How much tile damage will we do?
    [DataField("tileIntensity")]
    private float _tileIntensity = 10f;

    /// <summary>
    /// Explodes the given Entity. If a PowerBooster Chromosome is present on the Gene then
    /// the Intensity and Tile Intensity is doubled while the slope is halved to increase the range
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="chromosomes"></param>
    public override void ApplyGeneEffect(Entity<SharedGeneHostComponent> entity, Dictionary<Chromosome, bool> chromosomes)
    {
        base.ApplyGeneEffect(entity, chromosomes);

        _explosionSystem ??= _entityManager.System<SharedExplosionSystem>();

        var powerBoost = 1;
        if (chromosomes[Chromosome.PowerBooster])
            powerBoost = 2;

        _explosionSystem.QueueExplosion(
            entity,
            _typeId,
            _intensity * powerBoost,
            _slope / powerBoost,
            _tileIntensity * powerBoost
            );
    }
}
