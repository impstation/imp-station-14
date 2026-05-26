using Content.Shared._Impstation.BloodlessChimp;
using Content.Shared._Impstation.BloodlessChimp.Components;
using Content.Shared.NukeOps;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client._Impstation.BloodlessChimp;

public sealed class FearRadiusTargetSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IClientNetManager _net = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<MakeTargetEvent>(OnMakeTargetEvent);
        SubscribeLocalEvent<FearRadiusTargetComponent,StartCollideEvent>(OnTargetStartCollide);

    }

    private void OnMakeTargetEvent(MakeTargetEvent ev)
    {
        var target = _entityManager.GetEntity(ev.Target);
        EnsureComp<FearRadiusTargetComponent>(target, out var fearRadius);
        fearRadius.CooldownTime = ev.CooldownTime;
        fearRadius.Warnings = ev.Warnings;
    }


    private void OnTargetStartCollide(Entity<FearRadiusTargetComponent> ent, ref StartCollideEvent args)
    {
        if (ent.Comp.FixtureID != args.OtherFixtureId)
            return;
        if(ent.Comp.CurrentCooldown >= _timing.CurTime)
            return;
        var warning = ent.Comp.Warnings[_random.Next(ent.Comp.Warnings.Count)];

        _popup.PopupEntity(Loc.GetString(warning),
            ent.Owner,
            ent.Owner,
            type: PopupType.MediumCaution);
        ent.Comp.CurrentCooldown = _timing.CurTime+ ent.Comp.CooldownTime;
    }
}
