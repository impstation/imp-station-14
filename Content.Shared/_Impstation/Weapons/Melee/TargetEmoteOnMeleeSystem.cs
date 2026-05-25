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
        // todo: add handling for randomized list emotes (maybe)
        // todo: add handling for chat printed emotes

        if (!args.HitEntities.Any())
            return;

        if (hitter.Comp.Emote == null)
            return;

        DebugTools.Assert(_protoMan.HasIndex(hitter.Comp.Emote), "Prototype not found. Did you make a typo?");

        foreach (var hitEnt in args.HitEntities)
        {
            if (TryComp<MobStateComponent>(hitEnt, out var stateComp)
            && stateComp.CurrentState == MobState.Alive)
            {
                _chatSystem.TryEmoteWithoutChat(hitEnt, hitter.Comp.Emote);
            }
        }
    }
}
