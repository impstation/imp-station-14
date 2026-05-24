using Content.Shared.Chat;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Content.Shared.Timing;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared._Impstation.Weapons.Melee.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Weapons.Melee;

public sealed class TargetEmoteOnMeleeSystem : EntitySystem
{
    [Dependency] private readonly SharedChatSystem _chatSystem = default!;
    [Dependency] private readonly UseDelaySystem _delay = default!;
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

        if (hitter.Comp.Emote == null)
            return;

        if (!_protoMan.HasIndex(hitter.Comp.Emote))
            return;

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
