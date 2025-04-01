using System.Linq;
using Content.Server.Database;
using Content.Server.Preferences.Managers;
using Content.Shared.Body.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mind;
using Content.Shared.Preferences;
using Content.Shared.Tag;
using Content.Shared.Traits;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Impstation.TraitRandomizer;

public sealed partial class TraitRandomizerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;
    [Dependency] private readonly SharedHandsSystem _sharedHandsSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TraitRandomizerComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<TraitRandomizerComponent> ent, ref MapInitEvent args)
    {
        if (!_mind.TryGetMind(ent, out _, out var mindComponent) || mindComponent.Session == null)
            return;

        var allTraits = _prototypeManager.EnumeratePrototypes<TraitPrototype>().ToList();
        List<TraitPrototype> traits = [];

        // make a list of the traits we should be adding 
        foreach (var trait in allTraits)
        {
            if (ent.Comp.Fonts && trait.Category == _prototypeManager.Index<TraitCategoryPrototype>("SignatureFonts"))
                traits.Add(trait);

            if (ent.Comp.Accents && trait.Category == _prototypeManager.Index<TraitCategoryPrototype>("SpeechTraits"))
                traits.Add(trait);

            if (ent.Comp.Quirks && trait.Category == _prototypeManager.Index<TraitCategoryPrototype>("Quirks"))
                traits.Add(trait);

            if (ent.Comp.Disabilities && trait.Category == _prototypeManager.Index<TraitCategoryPrototype>("Disabilities"))
                traits.Add(trait);

        }

        var curProfile = (HumanoidCharacterProfile)_prefs.GetPreferences(mindComponent.Session.UserId).SelectedCharacter;

        var curTraits = curProfile.TraitPreferences.ToList();

        // remove currently applied traits from the list of traits we can roll from.
        foreach (var traitProto in curTraits)
        {
            var trait = _prototypeManager.Index(traitProto);
            traits.Remove(trait);
        }

        // how many traits are we gonna get?
        var traitsToRoll = _random.Next(ent.Comp.MinTraits, ent.Comp.MaxTraits);
        List<TraitPrototype> finalTraits = [];

        // pick a trait, ensure we don't pick it again, and add it to the final traits list. do this that many times.
        // note: currently this ignores points limits, because I think it's funnier that way. 
        for (var i = 0; i < traitsToRoll; i++)
        {
            var thisTrait = _random.Pick(traits);
            allTraits.Remove(thisTrait);
            finalTraits.Add(thisTrait);
        }

        foreach (var traitId in finalTraits)
        {
            if (!_prototypeManager.TryIndex<TraitPrototype>(traitId, out var traitPrototype))
            {
                Log.Warning($"No trait found with ID {traitId}!");
                return;
            }

            if (_whitelistSystem.IsWhitelistFail(traitPrototype.Whitelist, ent) ||
                _whitelistSystem.IsBlacklistPass(traitPrototype.Blacklist, ent))
                continue;

            // Add all components required by the prototype to the body or specified organ
            if (traitPrototype.Organ != null)
            {
                foreach (var organ in _bodySystem.GetBodyOrgans(ent))
                {
                    if (traitPrototype.Organ is { } organTag && _tagSystem.HasTag(organ.Id, organTag))
                    {
                        EntityManager.AddComponents(organ.Id, traitPrototype.Components);
                    }
                }
            }
            else
            {
                EntityManager.AddComponents(ent, traitPrototype.Components, false);
            }

            // Add item required by the trait
            if (traitPrototype.TraitGear == null)
                continue;

            if (!TryComp(ent, out HandsComponent? handsComponent))
                continue;

            var coords = Transform(ent).Coordinates;
            var inhandEntity = EntityManager.SpawnEntity(traitPrototype.TraitGear, coords);
            _sharedHandsSystem.TryPickup(ent,
                inhandEntity,
                checkActionBlocker: false,
                handsComp: handsComponent);
        }
    }
}