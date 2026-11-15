using System.Linq;
using Content.Server.Body;
using Content.Shared.Humanoid;
using Content.Shared._DV.Medical;
using Content.Shared._DV.Traits;

namespace Content.Server._DV.Medical;

/// <summary>
///     System to handle hormonal effects
/// </summary>
public sealed partial class HormoneSystem : EntitySystem
{
    [Dependency] private HumanoidProfileSystem _humanoidSystem = default!;
    [Dependency] private VisualBodySystem _visualBody = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FeminizedComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<FeminizedComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<MasculinizedComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<MasculinizedComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnInit(EntityUid uid, IHormoneComponent component, ComponentInit args)
    {
        if (!TryComp<HumanoidProfileComponent>(uid, out var humanoid) || humanoid.Sex == component.Target) // Imp - Resolve would cause a test fail
            return;

        if (!TryComp<HormoneSensitiveComponent>(uid, out var trait) || trait.Target != component.Target)
            return;

        component.Original = humanoid.Sex;
        if (!_visualBody.TryGatherMarkingsData(uid, null, out var data, out _, out _))
            return;

        var transProfile = data.ToDictionary(pair => pair.Key,
            pair => pair.Value with { Sex = component.Target });

        _visualBody.ApplyProfiles(uid, transProfile);
    }

    private void OnShutdown(EntityUid uid, IHormoneComponent component, ComponentShutdown args)
    {
        if (component.Original == null) // Imp - Resolve would cause a test fail
            return;

        _humanoidSystem.ApplySex(uid, component.Original.Value);
        if (!_visualBody.TryGatherMarkingsData(uid, null, out var data, out _, out _))
            return;

        var transProfile = data.ToDictionary(pair => pair.Key,
            pair => pair.Value with { Sex = component.Original.Value });

        _visualBody.ApplyProfiles(uid, transProfile);
    }
}
