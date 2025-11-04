using Content.Shared._Impstation.Xenoarchaeology.Artifact.XAE.Components;
using Content.Shared.Forensics;
using Content.Shared.Forensics.Components;
using Content.Shared.Forensics.Systems;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Preferences;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Impstation.Xenoarchaeology.Artifact.XAE.Systems;

public sealed class XAEScrambleDNASystem : BaseXAESystem<XAEScrambleDNAComponent>
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedForensicsSystem _forensicsSystem = default!;
    [Dependency] private readonly IdentitySystem _identity = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly HashSet<EntityUid> _entitiesInRange = new();

    protected override void OnActivated(Entity<XAEScrambleDNAComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var dnaScrambleComponent = ent.Comp;
        _entitiesInRange.Clear();
        _lookup.GetEntitiesInRange(ent.Owner, dnaScrambleComponent.Radius, _entitiesInRange);
        var count = 1;
        foreach (var entityInRange in _entitiesInRange)
        {
            ScrambleTargetDNA(entityInRange, ent.Comp);

            count += 1;
            if (count > dnaScrambleComponent.Count)
                return;
        }
    }

    private void ScrambleTargetDNA(EntityUid target, XAEScrambleDNAComponent component)
    {
        // TODO: Implement cross-species transformation (requires de-transforms from all species and transforms to all species)
        if (TryComp<HumanoidAppearanceComponent>(target, out var humanoid))
        {
            var newProfile = HumanoidCharacterProfile.RandomWithSpecies(humanoid.Species);
            newProfile.Gender = humanoid.Gender; // No Gender dysphoria please
            newProfile.Sex = humanoid.Sex; // No Sex dysphoria either
            _humanoidAppearance.LoadProfile(target, newProfile, humanoid);
            _metaData.SetEntityName(target, newProfile.Name, raiseEvents: false); //NT systems recognise you as someone else
            if (HasComp<DnaComponent>(target))
                _forensicsSystem.RandomizeDNA(target);

            if (HasComp<FingerprintComponent>(target))
                _forensicsSystem.RandomizeFingerprint(target);
            _identity.QueueIdentityUpdate(target); // manually queue identity update since we don't raise the event
        }
    }
}
