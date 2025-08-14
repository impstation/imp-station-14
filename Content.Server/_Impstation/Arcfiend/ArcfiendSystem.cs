using Content.Server.Actions;
using Content.Server.Body.Systems;
using Content.Server.DoAfter;
using Content.Server.Emp;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Flash;
using Content.Server.Gravity;
using Content.Server.Humanoid;
using Content.Server.Light.EntitySystems;
using Content.Server.Objectives.Components;
using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Server.Popups;
using Content.Server.Store.Systems;
using Content.Server.Stunnable;
using Content.Server.Zombies;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Camera;
using Content.Shared.Arcfiend;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Flash.Components;
using Content.Shared.Fluids;
using Content.Shared.Forensics.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Jittering;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Polymorph;
using Content.Shared.Popups;
using Content.Shared.Revolutionary.Components;
using Content.Shared.StatusEffect;
using Content.Shared.Stealth.Components;
using Content.Shared.Store.Components;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Timing;
using System.Linq;
using System.Numerics;

namespace Content.Server.Arcfiend;

public sealed partial class ArcfiendSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArcfiendComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ArcfiendComponent, ComponentRemove>(OnComponentRemove);

        SubscribeAbilities();
    }

    private void UpdateEnergy(EntityUid uid, ArcfiendComponent comp, float amount)
    {
        var energy = comp.Energy;
        energy += amount;
        comp.Energy = Math.Clamp(energy, 0, comp.MaxEnergy);
        Dirty(uid, comp);
        _alerts.ShowAlert(uid, "ArcfiendEnergy");
    }

    #region Event Handlers

    private void OnStartup(EntityUid uid, ArcfiendComponent comp, ref ComponentStartup args)
    {
        EnsureComp<ZombieImmuneComponent>(uid);

        // add actions
        foreach (var actionId in comp.ArcfiendActions)
            _actions.AddAction(uid, actionId);

        // make sure they start with zero energy
        comp.Energy = 0;

        // show alert
        UpdateEnergy(uid, comp, 0);
    }
    private void OnComponentRemove(Entity<ArcfiendComponent> ent, ref ComponentRemove args)
    {
        //might have to do stuff here later
    }
    #endregion
}
