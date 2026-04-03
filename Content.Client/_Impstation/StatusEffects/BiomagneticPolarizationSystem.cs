using Content.Client.Audio;
using Content.Shared._Impstation.StatusEffectNew;
using Content.Shared.Audio;
using Content.Shared.StatusEffectNew.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Physics.Components;

namespace Content.Client._Impstation.StatusEffects;

public sealed class BiomagneticPolarizationSystem : SharedBiomagneticPolarizationSystem
{
    [Dependency] private readonly AmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BiomagneticPolarizationStatusEffectComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<BiomagneticPolarizationStatusEffectComponent, ComponentShutdown>(OnShutdown);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<BiomagneticPolarizationStatusEffectComponent>();
        while (query.MoveNext(out var ent, out var comp))
        {
            // skip all the sprite shit if capped status hasn't changed since last frame, so we're only doing it once.
            if (comp.Capped == comp.LastCapped)
                continue;

            var ambientComp = EnsureComp<AmbientSoundComponent>(ent);

            if (comp.Capped)
            {
                SetCappedSprite((ent, comp), true);
                _ambientSound.SetAmbience(ent, true, ambientComp);
            }
            else
            {
                SetCappedSprite((ent, comp), false);
                _ambientSound.SetAmbience(ent, false, ambientComp);
            }
        }

        base.Update(frameTime);
    }

    public void OnInit(Entity<BiomagneticPolarizationStatusEffectComponent> ent, ref ComponentInit args)
    {
        if (!TryComp<StatusEffectComponent>(ent, out var statusEffect))
            return;

        if (!TryComp<PhysicsComponent>(statusEffect.AppliedTo, out var physComp) || statusEffect.AppliedTo is not { } appliedTo)
            return;

        Entity<PhysicsComponent?>? entPhys = (appliedTo, physComp);

        ent.Comp.StatusOwner = entPhys;
    }

    public void OnShutdown(Entity<BiomagneticPolarizationStatusEffectComponent> ent, ref ComponentShutdown args)
    {
        SetCappedSprite(ent, false);
    }

    public void SetCappedSprite(Entity<BiomagneticPolarizationStatusEffectComponent> ent, bool setting)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;
        _sprite.LayerSetVisible((ent, sprite), BiomagneticPolarizationLayers.Capped, setting);
    }
}
