using System.Linq;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

public sealed class FoamArtifactSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SmokeSystem _smoke = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<FoamArtifactComponent, ArtifactNodeEnteredEvent>(OnNodeEntered);
        SubscribeLocalEvent<FoamArtifactComponent, ArtifactActivatedEvent>(OnActivated);
    }

    private void OnNodeEntered(Entity<FoamArtifactComponent> ent, ref ArtifactNodeEnteredEvent args)
    {
        if (!(ent.Comp.Reagents.Count != 0))
            return;

        ent.Comp.SelectedReagent = ent.Comp.Reagents[args.RandomSeed % ent.Comp.Reagents.Count];
    }

    private void OnActivated(Entity<FoamArtifactComponent> ent, ref ArtifactActivatedEvent args)
    {
        if (ent.Comp.SelectedReagent == null)
            return;

        var sol = new Solution();
        var xform = Transform(ent);
        var range = (int) MathF.Round(MathHelper.Lerp(ent.Comp.MinFoamAmount, ent.Comp.MaxFoamAmount, _random.NextFloat(0, 1f)));
        sol.AddReagent(ent.Comp.SelectedReagent, ent.Comp.ReagentAmount);
        var foamEnt = Spawn("Foam", xform.Coordinates);
        var spreadAmount = range * 4;
        _smoke.StartSmoke(foamEnt, sol, ent.Comp.Duration, spreadAmount);
    }
}