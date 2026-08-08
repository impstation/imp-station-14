using Content.Shared.Tag;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared._Impstation.Weapons.Ranged.Components;
using Content.Shared._Impstation.Weapons.Ranged.Events;

namespace Content.Shared._Impstation.Weapons.Ranged.Systems;

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

    private void OnGetProjectileCount(Entity<GunProjectileCountModifierComponent> ent, ref GunGetAmmoProjectileCountEvent args)
    {
        var countModifier = ent.Comp.ProjCount;

        foreach (var (tag, count) in ent.Comp.ProjCountSpecific)
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
