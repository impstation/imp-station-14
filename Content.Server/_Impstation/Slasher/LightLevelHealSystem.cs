using Content.Server.Body.Systems;
using Content.Server._Impstation.Lighting;
using Content.Shared.Body.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared._Impstation.Slasher;
using Content.Shared._Impstation.Slasher.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Slasher;

/// <summary>
/// Resolves the Slasher's dark-heal action.
/// The heal only succeeds when the user's current tile is below the configured luminance threshold after the nearby light sample is clamped to the server's PVS distance rules.
/// </summary>
public sealed class LightLevelHealSystem : EntitySystem
{
    private const string DarkHealFailLoc = "slasher-dark-heal-fail";
    private const string DarkHealNoDamageLoc = "slasher-dark-heal-no-damage";
    /// <summary>
    /// Damage groups affected by dark heal.
    /// </summary>
    private static readonly ProtoId<DamageGroupPrototype>[] HealedDamageGroups =
    {
        "Brute",
        "Burn",
        "Toxin",
        "Airloss",
    };

    [Dependency] private readonly LuminanceAtCoordinateSystem _luminance = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    /// <summary>
    /// Subscribes dark-heal action event handling.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LightLevelHealActionComponent, SlasherDarkHealEvent>(OnDarkHeal);
    }

    /// <summary>
    /// Handles dark-heal action usage by validating luminance and applying grouped healing.
    /// </summary>
    /// <param name="ent">Action entity and its configured component values.</param>
    /// <param name="args">Action event data for the current use.</param>
    private void OnDarkHeal(Entity<LightLevelHealActionComponent> ent, ref SlasherDarkHealEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<DamageableComponent>(args.Performer, out var damageable))
            return;

        if (damageable.TotalDamage <= FixedPoint2.Zero)
        {
            _popup.PopupEntity(Loc.GetString(DarkHealNoDamageLoc), args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        var performerCoords = _xform.GetMapCoordinates(args.Performer);
        var luminanceResult = _luminance.Evaluate(performerCoords,
            ent.Comp.AmbientDarkThreshold);

        if (!luminanceResult.MeetsThreshold)
        {
            _popup.PopupEntity(Loc.GetString(DarkHealFailLoc), args.Performer, args.Performer, PopupType.MediumCaution);
            return;
        }

        var remaining = FixedPoint2.New(ent.Comp.HealAmount);
        var remainingGroups = HealedDamageGroups.Length;

        foreach (var group in HealedDamageGroups)
        {
            if (remaining <= FixedPoint2.Zero)
                break;

            var groupHeal = remaining / remainingGroups;
            remaining -= groupHeal;
            remainingGroups--;

            _damageable.HealEvenly((args.Performer, damageable), -groupHeal, group, args.Performer);
        }

        if (TryComp<BloodstreamComponent>(args.Performer, out var bloodstream) && bloodstream.BleedAmount > 0)
            _bloodstream.TryModifyBleedAmount((args.Performer, bloodstream), ent.Comp.BleedClosureAmount);

        args.Handled = true;
    }
}