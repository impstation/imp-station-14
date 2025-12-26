using Content.Shared.Tag;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared.Weapons.Ranged.Systems;

/// <summary>
/// Adds/removes projectiles for guns that use cartridge-based projectiles.
/// </summary>
public sealed class GunProjectileCountModifierSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tagSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GunProjectileCountModifierComponent, GunGetAmmoProjectileCountEvent>(OnGetProjectileCount);
    }

    private void OnGetProjectileCount(EntityUid uid, GunProjectileCountModifierComponent component, ref GunGetAmmoProjectileCountEvent args)
    {
        var countModifier = component.ProjCount;

        foreach (var (tag, count) in component.ProjCountSpecific)
        {
            if (_tagSystem.HasTag(args.AmmoEntity, tag))
            {
                countModifier = count;
                break;
            }
        }

        args.Count += countModifier;
    }
}
