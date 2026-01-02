using Content.Server.Ghost.Roles.Components;
using Content.Server.Speech.Components;
using Content.Shared.EntityEffects;
using Content.Shared.Mind.Components;
using Content.Shared._Impstation.EntityEffects.Effects;
using Content.Shared.Humanoid;

namespace Content.Server._Impstation.EntityEffects.Effects;

// Code largely compied from MakeSentientEntityEffectSystem
/// <summary>
/// Makes this entity sentient and required to follow orders from a syndicate agent. Allows ghost to take it over if it's not already occupied.
/// Optionally also allows this entity to speak.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class MakeSyndientEntityEffectSystem : EntityEffectSystem<MetaDataComponent, MakeSyndient>
{
    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<MakeSyndient> args)
    {
        // Let affected entities speak normally to make this effect different from, say, the "random sentience" event
        // This also works on entities that already have a mind
        // We call this before the mind check to allow things like player-controlled mice to be able to benefit from the effect
        if (args.Effect.AllowSpeech)
        {
            RemComp<ReplacementAccentComponent>(entity);
            RemComp<MonkeyAccentComponent>(entity);
        }

        // Stops from adding a ghost role to things like people who already have a mind
        if (TryComp<MindContainerComponent>(entity, out var mindContainer) && mindContainer.HasMind ||
        //slightly hacky way to make sure it doesn't work on humanoid ghost roles that haven't been claimed yet
            HasComp<HumanoidAppearanceComponent>(entity))
        {
            return;
        }

        // in an ideal world, this is where we would get the name of the injector to display as ghost role text.

        var ghostRole = EnsureComp<GhostRoleComponent>(entity);
        EnsureComp<GhostTakeoverAvailableComponent>(entity);

        ghostRole.RoleName = entity.Comp.EntityName;
        ghostRole.RoleDescription = Loc.GetString("ghost-role-information-subjuzine-description");
        ghostRole.RoleRules = Loc.GetString("ghost-role-information-subjuzine-rules");

        //if there already was a ghost role, change the role description and rules to make it clear it's been injected with subjuzine
        Dirty(entity, ghostRole);

        // TODO: give the entity some way to identify who injected it. and don't do it using reagentsystem.
        // in memoriam jungle juice 2/10/2024-8/7/2025
    }
}
