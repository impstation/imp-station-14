using System.Numerics;
using Content.Client.Audio;
using Content.Shared._Impstation.CCVar;
using Content.Shared._Impstation.StatusEffectNew;
using Content.Shared.Audio;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Stealth.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Physics.Components;
using Robust.Shared.Utility;

namespace Content.Client._Impstation.StatusEffects;

public sealed class BiomagneticPolarizationSystem : SharedBiomagneticPolarizationSystem
{
    [Dependency] private readonly AmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    private static readonly ResPath SpriteRSIPath = new("/Textures/Effects/text.rsi");
    private static readonly SpriteSpecifier.Rsi Plus = new(SpriteRSIPath, "plus");
    private static readonly SpriteSpecifier.Rsi Dash = new(SpriteRSIPath, "dash");
    private const float WidthUntilSprite = 1f / EyeManager.PixelsPerMeter * 1f; // 1f multiplication is the pixel difference from the right of the png to the actual sprite
    private const float HeightUntilSprite = 1f / EyeManager.PixelsPerMeter * 1f; // 1f multiplication is the pixel difference from the top of the png to the actual sprite

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BiomagneticPolarizationStatusEffectComponent, AfterAutoHandleStateEvent>(OnHandleState);
        SubscribeLocalEvent<BiomagneticPolarizationStatusEffectComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<BiomagneticPolarizationStatusEffectComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BiomagneticPolarizationStatusEffectComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnHandleState(Entity<BiomagneticPolarizationStatusEffectComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        AdjustSign(ent);

        var ambientComp = EnsureComp<AmbientSoundComponent>(ent);

        if (ent.Comp.Capped)
        {
            SetCappedSprite(ent, true);
            _ambientSound.SetAmbience(ent, true, ambientComp);
        }
        else
        {
            SetCappedSprite(ent, false);
            _ambientSound.SetAmbience(ent, false, ambientComp);
        }
    }

    private void OnInit(Entity<BiomagneticPolarizationStatusEffectComponent> ent, ref ComponentInit args)
    {
        if (!TryComp<StatusEffectComponent>(ent, out var statusEffect))
            return;

        if (!TryComp<PhysicsComponent>(statusEffect.AppliedTo, out var physComp) || statusEffect.AppliedTo is not { } appliedTo)
            return;

        Entity<PhysicsComponent?>? entPhys = (appliedTo, physComp);

        ent.Comp.StatusOwner = entPhys;
    }

    private void OnShutdown(Entity<BiomagneticPolarizationStatusEffectComponent> ent, ref ComponentShutdown args)
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

        var sign = ent.Comp.Polarization ? Plus : Dash;
        var layer = _sprite.AddLayer((ent, sprite), sign);

        _sprite.LayerMapSet((ent, sprite), BiomagneticPolarizationSignKey.Key, layer);
    }

    private void AdjustSign(Entity<BiomagneticPolarizationStatusEffectComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (!_sprite.TryGetLayer((ent, sprite), BiomagneticPolarizationSignKey.Key, out var layer, false))
            return;

        if (!_configManager.GetCVar(ImpCCVars.EnableBiomagneticPolarizationSymbols)
            || TryComp<StatusEffectComponent>(ent, out var statusEffect)
            && TryComp<StealthComponent>(statusEffect.AppliedTo, out var stealth)
            && stealth.Enabled)
        {
            _sprite.LayerSetVisible(layer, false);
            return;
        }
        else
            _sprite.LayerSetVisible(layer, true);

        var logStrength = MathF.Log(1f + ent.Comp.CurrentStrength);
        var scale = 8f + logStrength;
        var scaling = new Vector2(scale, scale);

        _sprite.LayerSetScale(layer, scaling);

        var offset = new Vector2(scaling.X * WidthUntilSprite / 2f, -(scaling.Y * HeightUntilSprite / 2f));

        _sprite.LayerSetOffset(layer, offset);

        var color = ent.Comp.Polarization ? ent.Comp.NorthColor : ent.Comp.SouthColor;
        var transparency = (float)Math.Min(0.15f + logStrength / 7.5f, 0.6f);

        _sprite.LayerSetColor(layer, color.WithAlpha(transparency));
    }

    private void RemoveSign(Entity<BiomagneticPolarizationStatusEffectComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (!_sprite.LayerMapTryGet((ent, sprite), BiomagneticPolarizationSignKey.Key, out var layer, false))
            return;

        _sprite.RemoveLayer((ent, sprite), layer);
    }

    private void SetCappedSprite(Entity<BiomagneticPolarizationStatusEffectComponent> ent, bool setting)
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
