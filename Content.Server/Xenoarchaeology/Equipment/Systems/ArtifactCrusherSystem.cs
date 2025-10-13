using Content.Server.Body.Systems;
using Content.Server.Stack;
using Content.Server.Xenoarchaeology.XenoArtifacts; //imp
using Content.Shared._Goobstation.Changeling; //imp
using Content.Shared.Body.Components;
using Content.Shared.Storage.Components;
using Content.Shared.Whitelist;
using Content.Shared.Xenoarchaeology.Equipment;
using Content.Shared.Xenoarchaeology.Equipment.Components;
using Robust.Shared.Collections;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.Equipment.Systems;

/// <inheritdoc/>
public sealed class ArtifactCrusherSystem : SharedArtifactCrusherSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly ArtifactSystem _artifact = default!; //#IMP

    // TODO: Move to shared once StackSystem spawning is in Shared and we have RandomPredicted
    public override void FinishCrushing(Entity<ArtifactCrusherComponent, EntityStorageComponent> ent)
    {
        var (_, crusher, storage) = ent;
        StopCrushing((ent, ent.Comp1), false);
        AudioSystem.PlayPvs(crusher.CrushingCompleteSound, ent);
        crusher.CrushingSoundEntity = null;
        Dirty(ent, ent.Comp1);

        var contents = new ValueList<EntityUid>(storage.Contents.ContainedEntities);
        var coords = Transform(ent).Coordinates;
        foreach (var contained in contents)
        {
            if (_whitelistSystem.IsWhitelistPass(crusher.CrushingWhitelist, contained))
            {
                var amount = _random.Next(crusher.MinFragments, crusher.MaxFragments);
                var stacks = _stack.SpawnMultiple(crusher.FragmentStackProtoId, amount, coords);
                foreach (var stack in stacks)
                {
                    ContainerSystem.Insert((stack, null, null, null), crusher.OutputContainer);
                }
            }

            //#IMP
            if (HasComp<ArtifactComponent>(contained))
                _artifact.ForceActivateArtifact(contained);

            if (!TryComp<BodyComponent>(contained, out var body))
                Del(contained);

            if (!HasComp<GoobChangelingComponent>(contained)) //#IMP if statement to make changelings immune
            {
                var gibs = _body.GibBody(contained, body: body, gibOrgans: true);
                foreach (var gib in gibs)
                {
                    ContainerSystem.Insert((gib, null, null, null), crusher.OutputContainer);
                }
            }
            else
            {
                _damageable.TryChangeDamage(contained, crusher.CrushingDamage * crusher.NonGibbedDamageMult); //#IMP still damage changelings a little
            }
        }
    }
}
