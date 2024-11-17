using Content.Server.Popups;
using Content.Server._Impstation.Cosmiccult.Components;
using Content.Shared._Impstation.Cosmiccult.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Mind;
using Content.Shared.Examine;
using Content.Server.Actions;
using Robust.Shared.Prototypes;
using Content.Shared.Alert;
using Robust.Shared.Random;
using Robust.Shared.Audio.Systems;
using Content.Server.Chat.Systems;

namespace Content.Server._Impstation.Cosmiccult;

public sealed partial class CosmicCultSystem : EntitySystem
{
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IRobustRandom _rand = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _aud = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public EntProtoId CultToolPrototype = "AbilityCosmicCultTool";
    public int ObjectiveEntropyTracker = 0;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CosmicCultComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<CosmicCultComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<CosmicItemComponent, ExaminedEvent>(OnCosmicItemExamine);
        // SubscribeLocalEvent<CosmicCultComponent, ComponentRemove>(OnComponentRemove); || We'll probably need this later.

        SubscribeAbilities();
    }

    /// <summary>
    /// Called when the component initializes. We add the Visibility Mask here.
    /// </summary>
    private void OnCompInit(Entity<CosmicCultComponent> ent, ref ComponentInit args)
    {

        if (TryComp<EyeComponent>(ent, out var eye))
            _eye.SetVisibilityMask(ent, eye.VisibilityMask | CosmicMonumentComponent.LayerMask);
    }

    /// <summary>
    /// Called when the component starts up, add the Cosmic Cult abilities to the user.
    /// </summary>
    private void OnStartup(EntityUid uid, CosmicCultComponent comp, ref ComponentStartup args)
    {
        foreach (var actionId in comp.BaseCosmicCultActions)
            _actions.AddAction(uid, actionId);
    }

    /// <summary>
    /// Called by Cosmic Siphon. Increments the Cult's global objective tracker.
    /// </summary>
    private void IncrementCultObjectiveEntropy()
    {
        ObjectiveEntropyTracker++;
    }

    /// <summary>
    /// A blacklist called when someone examines an object with the CosmicItem Component.
    /// </summary>
    private void OnCosmicItemExamine(Entity<CosmicItemComponent> ent, ref ExaminedEvent args)
    {
        if (HasComp<CosmicCultComponent>(args.Examiner))
            return;

        args.PushMarkup(Loc.GetString("contraband-object-text-cosmiccult"));
    }

}
