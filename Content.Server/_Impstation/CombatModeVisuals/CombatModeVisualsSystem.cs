using Content.Shared._Impstation.CombatModeVisuals;
using Content.Shared.CombatMode;
using Content.Shared.Mobs;
using Robust.Server.GameObjects;

namespace Content.Server._Impstation.CombatModeVisuals;

public sealed partial class CombatModeVisualsSystem : SharedCombatModeVisualsSystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CombatModeVisualsComponent, ToggleCombatActionEvent>(OnCombatToggle);
        SubscribeLocalEvent<CombatModeVisualsComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnCombatToggle(Entity<CombatModeVisualsComponent> ent, ref ToggleCombatActionEvent args)
    {
        if (TryComp<CombatModeComponent>(ent, out var combat))
            _appearance.SetData(ent, CombatModeVisualsVisuals.Combat, combat.IsInCombatMode);
    }

    private void OnMobStateChanged(Entity<CombatModeVisualsComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Critical || args.NewMobState == MobState.Dead)
            _appearance.SetData(ent, CombatModeVisualsVisuals.Combat, false);
    }
}
