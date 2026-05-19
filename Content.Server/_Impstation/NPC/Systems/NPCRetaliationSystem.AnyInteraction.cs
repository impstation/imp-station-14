using Content.Server.NPC.Components;
using Content.Shared.Interaction;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Hands;
using Content.Shared.Pointing;

namespace Content.Server.NPC.Systems;

/// <summary>
///     Handles NPC which become aggressive after being attacked.
/// </summary>
public sealed partial class NPCRetaliationSystem
{
    private void OnPull(Entity<NPCRetaliationComponent> ent, ref PullStartedMessage args)
    {
        if (!ent.Comp.AnyInteraction)
            return;
        TryRetaliate(ent, args.PullerUid);
    }

    private void OnAttack(Entity<NPCRetaliationComponent> ent, ref AttackedEvent args)
    {
        if (!ent.Comp.AnyInteraction)
            return;
        TryRetaliate(ent, args.User);
    }

    private void OnPickup(Entity<NPCRetaliationComponent> ent, ref GotEquippedHandEvent args)
    {
        if (!ent.Comp.AnyInteraction)
            return;
        if (args.Handled)
            return;
        args.Handled = TryRetaliate(ent, args.User);
    }

    private void OnPointedAt(Entity<NPCRetaliationComponent> ent, ref AfterGotPointedAtEvent args)
    {
        if (!ent.Comp.AnyInteraction)
            return;
        TryRetaliate(ent, args.Pointer);
    }

    private void OnAfterInteract(Entity<NPCRetaliationComponent> ent, ref ActivateInWorldEvent args)
    {
        if (!ent.Comp.AnyInteraction)
            return;
        TryRetaliate(ent, args.User);
    }
}
