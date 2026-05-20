using Content.Server.Antag;
using Content.Server.GameTicking.Rules;
using Content.Server.Humanoid;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.GameTicking.Rules;

/// <summary>
/// Makes this rules antags spawn with a random humanoid profile.
/// </summary>
public sealed class AntagRandomHumanoidRuleSystem : GameRuleSystem<AntagRandomHumanoidRuleComponent>
{
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AntagRandomHumanoidRuleComponent, AntagSelectEntityEvent>(OnSelectEntity);
    }

    private void OnSelectEntity(Entity<AntagRandomHumanoidRuleComponent> ent, ref AntagSelectEntityEvent args)
    {
        if (args.Handled)
            return;

        var profile = HumanoidCharacterProfile.Random(false);
        var speciesProto = _proto.Index(profile.Species);
        args.Entity = Spawn(speciesProto.Prototype);

        _humanoid.LoadProfile(args.Entity.Value, profile);
    }
}
