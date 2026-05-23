using Content.Shared.Chat;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Content.Shared.Stunnable;
using Content.Shared.Timing;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared._Impstation.Weapons.Melee.Components;

namespace Content.Shared._Impstation.Weapons.Melee;

public sealed class EmoteOnHitSystem : EntitySystem
{
    [Dependency] private readonly SharedChatSystem _chatSystem = default!;
    [Dependency] private readonly UseDelaySystem _delay = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<EmoteOnHitComponent, MeleeHitEvent>(OnMeleeHit);
    }

    /// <summary>
    /// Tries to add an emote to a living targetted entity when hit.
    /// </summary>
    private void OnMeleeHit(Entity<EmoteOnHitComponent> hitter, ref MeleeHitEvent args)
    {
        // todo: add handling for randomized list emotes (maybe)
        // todo: add handling for chat printed emotes

        if (!args.IsHit)
            return;

        if (_delay.IsDelayed(hitter.Owner))
            return;

        if (args.HitEntities.Count == 0)
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
