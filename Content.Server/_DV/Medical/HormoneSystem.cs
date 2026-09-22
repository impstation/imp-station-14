using Content.Shared.Preferences;
using Content.Shared.Humanoid;
using Content.Shared._DV.Medical;
using Content.Shared._DV.Traits;

namespace Content.Server._DV.Medical;

/// <summary>
///     System to handle hormonal effects
/// </summary>
public sealed class HormoneSystem : EntitySystem
{
    [Dependency] private readonly HumanoidProfileSystem _humanoidProfile = default!;

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

        if (TryComp<HormoneSensitiveComponent>(uid, out var trait) && trait.Target == component.Target) {
            component.Original = humanoid.Sex;
            _humanoidSystem.SetSex(uid, component.Target);
        }
    }

    private void OnShutdown(EntityUid uid, IHormoneComponent component, ComponentShutdown args)
    {
        if (!TryComp<HumanoidProfileComponent>(uid, out _) || component.Original == null) // Imp - Resolve would cause a test fail
            return;

        _humanoidSystem.SetSex(uid, component.Original.Value);
    }
}
