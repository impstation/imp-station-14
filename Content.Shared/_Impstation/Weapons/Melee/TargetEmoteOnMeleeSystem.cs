using System.Linq;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared._Impstation.Weapons.Melee.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Impstation.Weapons.Melee;

public sealed class TargetEmoteOnMeleeSystem : EntitySystem
{
    [Dependency] private readonly SharedChatSystem _chatSystem = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TargetEmoteOnMeleeComponent, MeleeHitEvent>(OnMeleeHit);
    }

    /// <summary>
    /// Tries to add an emote to a living targetted entity when hit.
    /// </summary>
    private void OnMeleeHit(Entity<TargetEmoteOnMeleeComponent> hitter, ref MeleeHitEvent args)
    {
        if (!args.HitEntities.Any())
            return;

        if (hitter.Comp.Emote == null)
            return;

        if (!_protoMan.HasIndex(hitter.Comp.Emote))
        {
            DebugTools.Assert("Prototype not found. Did you make a typo?");
            return;
        }

        foreach (var hitEnt in args.HitEntities)
        {
            if (!TryComp<MobStateComponent>(hitEnt, out var stateComp))
                continue;

            if (stateComp.CurrentState != MobState.Alive)
                continue;

            if (hitter.Comp.PrintChat == true)
            {
                _chatSystem.TryEmoteWithChat(hitEnt, hitter.Comp.Emote);
            }
            else
            {
                _chatSystem.TryEmoteWithoutChat(hitEnt, hitter.Comp.Emote);
            }
        }
    }
}
