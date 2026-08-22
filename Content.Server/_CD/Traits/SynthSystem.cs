using Content.Server.Body.Systems;
using Content.Server.Database;
using Content.Shared.Body.Components; // Imp Edit
using Content.Shared.Chat.TypingIndicator;
using Content.Shared.Chemistry.Components; // Imp Edit
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Speech.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._CD.Traits;

public sealed class SynthSystem : EntitySystem
{
    // Begin DeltaV - make strings static readonly
    private static readonly ProtoId<TypingIndicatorPrototype> RobotTypingIndicator = "robot";
    // private static readonly ProtoId<ReagentPrototype> SynthBloodReagent = "SynthBlood"; // VDS - use solution in component instead.
    // End DeltaV

    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SynthComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, SynthComponent component, ComponentStartup args)
    {
        if (TryComp<TypingIndicatorComponent>(uid, out var indicator))
        {
            indicator.TypingIndicatorPrototype = RobotTypingIndicator; // DeltaV - make strings static readonly
            Dirty(uid, indicator);
        }
        // Imp Edit Start - Get the volume of the entity's bloodstream and generate a solution based on that.
        if (!TryComp<BloodstreamComponent>(uid, out var bloodStream))
            return;

        Solution bloodSolution = new(component.SynthBloodReagent, bloodStream.BloodReferenceSolution.Volume);
        // Imp Edit End

        // Give them synth blood. Ion storm notif is handled in that system
        _bloodstream.ChangeBloodReagents(uid, bloodSolution); // DeltaV - make strings static readonly
                                                              // VDS - update to use new ChangeBloodReagents
                                                              // IMP - component.SynthBloodReagent > bloodSolution
        // Gives them the DamagedSiliconAccent component
        EnsureComp<DamagedSiliconAccentComponent>(uid, out var accent);
        accent.EnableChargeCorruption = false; //Disables corruption on low battery. This would always be active since non-silicons don't have a battery
        accent.DamageAtMaxCorruption = 200; //This is makes it usable for anyone not a silicon
        Dirty(uid, accent);
    }
}
