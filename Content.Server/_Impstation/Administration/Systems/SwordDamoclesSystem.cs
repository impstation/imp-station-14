// using System.Numerics;
// // using Content.Client.Administration.Components;
// using Content.Shared._Impstation.Administration.Components;
// // using Robust.Client.GameObjects;
// using Robust.Shared.Utility;
// using Robust.Shared.Toolshed.Commands.Values;
// using Content.Shared.Damage.Systems;

// namespace Content.Server._Impstation.Administration.Systems;

// public abstract class SwordDamoclesSystem : EntitySystem // will be removed in favour of a single shared system
// {
//     // [Dependency] private readonly SpriteSystem _sprite = default!;

//     [Dependency] private readonly DamageableSystem _damage = default!;

//     public override void Initialize()
//     {
//         base.Initialize();

//         SubscribeLocalEvent<SwordDamoclesComponent, ComponentStartup>(OnAdded);
//         SubscribeLocalEvent<SwordDamoclesComponent, ComponentShutdown>(OnRemoved);
//     }

//     private void OnRemoved(Entity<SwordDamoclesComponent> ent, ref ComponentShutdown args)
//     {
//         ent.Comp.TimesApplied = 0; // reset after the component is removed, so it can be set up to go off again later // might actually not need this and have it go off by a modulo check instead
//     }

//     private void OnAdded(Entity<SwordDamoclesComponent> ent, ref ComponentStartup args)
//     {
//         if (ent.Comp.TimesApplied >= 1) // check if the smite has already been used on this person // might change this to a modulo check instead
//             _damage.TryChangeDamage(ent.Owner, ent.Comp.Damage, ignoreResistances: true, interruptsDoAfters: true); // do damage if so

//         ent.Comp.TimesApplied += 1; // increase the counter by one whether or not this is the first time
//     }
// }
