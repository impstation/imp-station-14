using Content.Server._Impstation.GameTicking.Rules.VariationPass.Components;
using Content.Server.GameTicking.Rules;

namespace Content.Server._Impstation.GameTicking.Rules.VariationPass;

/// <summary>
/// Handles modifying components on players.
/// </summary>
public sealed class ModifyComponentsVariationPassSystem : PlayerVariationPassSystem<ModifyComponentsVariationPassComponent>
{
    protected override void ApplyPlayerVariation(Entity<ModifyComponentsVariationPassComponent> ent, ref PlayerVariationPassEvent args)
    {
        var receivers = ent.Comp.MinMaxComponentRecievers.Next(Random);

        foreach (var player in GetPlayers(receivers, true, ref args))
        {
            EntityManager.AddComponents(player, ent.Comp.AddedComponents, ent.Comp.RemoveExisting);
            EntityManager.RemoveComponents(player, ent.Comp.RemovedComponents);
        }
    }
}
