using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Server._Impstation.Colonid.Components;
using Content.Shared.Atmos;
using Content.Shared.Inventory;
using static Content.Shared.Atmos.Components.GasAnalyzerComponent;

namespace Content.Server._Impstation.Colonid.EntitySystems;

public sealed class IgniteFromGasSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmo = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;

    private readonly Entity<IgniteFromGasComponent> _ent = default;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (CheckAtmosForGas() && !CheckInventoryForProtection())
        {
            _flammable.AdjustFireStacks(_ent, _ent.Comp.FireStacksAmount);
        }

    }

    /// <summary>
    ///     checks if the atmosphere the entity is in contains the gas specified in the component.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns> true or false </returns>
    private bool CheckAtmosForGas()
    {
        string targetGas = _ent.Comp.TriggeringGas;

        var gasAtTile = _atmo.GetContainingMixture(_ent, true);
        var gasList = GenerateGasEntryArray(gasAtTile);

        for (var i = 0; i < gasList.Count; i++)
        {
            if (gasList[i] == targetGas)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    ///     checks the entity's inventory to see if it's wearing protection in both the head slot and the inner/outer wear slot
    /// </summary>
    /// <param name="entity"></param>
    /// <returns> true or false </returns>
    private bool CheckInventoryForProtection(Entity<InventoryComponent> entity) // TODO: change "Inner Wear", etc to something.Comp.OuterWear, etc also has to find those entities first
    {
        bool output = false;

        if ((_inventorySystem.TryGetInventoryEntity<SealedClothingComponent>(entity, "Inner Wear") || _inventorySystem.TryGetInventoryEntity<SealedClothingComponent>(entity, "Outer Wear")) && _inventorySystem.TryGetInventoryEntity<SealedClothingComponent>(entity, "Helmet"))
        {
            output = true;
        }

        return output;
    }

    private GasEntry[] GenerateGasEntryArray(GasMixture? mixture)
    {
        var gases = new List<GasEntry>();

        for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
        {
            var gas = _atmo.GetGas(i);

            if (mixture?[i] <= 0.01)
                continue;

            if (mixture != null)
            {
                var gasName = Loc.GetString(gas.Name);
                gases.Add(new GasEntry(gasName, mixture[i], gas.Color));
            }
        }

        var gasesOrdered = gases.OrderByDescending(gas => gas.Amount);

        return gasesOrdered.ToArray();
    }
}
