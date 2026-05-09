using System.Numerics;
using Content.Client.Audio;
using Content.Shared._Impstation.StatusEffectNew;
using Content.Shared.Audio;
using Content.Shared.StatusEffectNew.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Utility;
using SixLabors.ImageSharp.ColorSpaces;

namespace Content.Client._Impstation.StatusEffects;

public sealed class BiomagneticPolarizationSystem : SharedBiomagneticPolarizationSystem
{
    [Dependency] private readonly AmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    private const string SpriteRSIPath = "/Textures/Effects/text.rsi";
    private const int PixelWidthUntilSprite = 1; // Pixel difference from the right of the png to the actual sprite
    private const int PixelHeightUntilSprite = 1; // Pixel difference from the top of the png to the actual sprite

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BiomagneticPolarizationStatusEffectComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<BiomagneticPolarizationStatusEffectComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BiomagneticPolarizationStatusEffectComponent, ComponentShutdown>(OnShutdown);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<BiomagneticPolarizationStatusEffectComponent>();
        while (query.MoveNext(out var ent, out var comp))
        {
            AdjustSign((ent, comp));

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
        RemoveSign(ent);
    }

    private void OnStartup(Entity<BiomagneticPolarizationStatusEffectComponent> ent, ref ComponentStartup args)
    {
        AddSign(ent);
    }

    private void AddSign(Entity<BiomagneticPolarizationStatusEffectComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (_sprite.LayerMapTryGet((ent, sprite), BiomagneticPolarizationSignKey.Key, out var _, false))
            return;

        var sign = ent.Comp.Polarization ? "plus" : "dash";
        var signSprite = new SpriteSpecifier.Rsi(new ResPath(SpriteRSIPath), sign);
        var layer = _sprite.AddLayer((ent, sprite), signSprite);

        _sprite.LayerMapSet((ent, sprite), BiomagneticPolarizationSignKey.Key, layer);
    }

    private void AdjustSign(Entity<BiomagneticPolarizationStatusEffectComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (!_sprite.LayerMapTryGet((ent, sprite), BiomagneticPolarizationSignKey.Key, out var layer, false))
            return;

        var comp = ent.Comp;

        var scale = 8f + MathF.Log(1f + comp.CurrentStrength);
        var scaling = new Vector2(scale, scale);

        _sprite.LayerSetScale((ent, sprite), layer, scaling);

        var widthUntilSprite = 1f / EyeManager.PixelsPerMeter * PixelWidthUntilSprite;
        var heightUntilSprite = 1f / EyeManager.PixelsPerMeter * PixelHeightUntilSprite;
        var offset = new Vector2(scaling.X * widthUntilSprite / 2f, -(scaling.Y * heightUntilSprite / 2f));

        _sprite.LayerSetOffset((ent, sprite), layer, offset);

        var color = comp.Polarization ? comp.NorthColor : comp.SouthColor;
        // var transparency = Math.Clamp(comp.CurrentStrength / 40f, 0.2f, 0.4f);

        var transparency = Math.Clamp(comp.CurrentStrength / 40f, 0.4f, 0.6f);

        _sprite.LayerSetColor((ent, sprite), layer, color.WithAlpha(transparency));
    }

    private void RemoveSign(Entity<BiomagneticPolarizationStatusEffectComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (!_sprite.LayerMapTryGet((ent, sprite), BiomagneticPolarizationSignKey.Key, out var layer, false))
            return;

        _sprite.RemoveLayer((ent, sprite), layer);
    }

    public void SetCappedSprite(Entity<BiomagneticPolarizationStatusEffectComponent> ent, bool setting)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;
        _sprite.LayerSetVisible((ent, sprite), BiomagneticPolarizationLayers.Capped, setting);
    }

    private enum BiomagneticPolarizationSignKey
    {
        Key,
    }
}
