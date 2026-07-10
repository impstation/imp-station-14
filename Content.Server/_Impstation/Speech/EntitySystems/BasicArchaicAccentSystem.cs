using Content.Server._Impstation.Speech.Components;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Speech;

namespace Content.Server._Impstation.Speech.EntitySystems;

public sealed class BasicArchaicAccentSystem : EntitySystem
{
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BasicArchaicAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(Entity<BasicArchaicAccentComponent> entity, ref AccentGetEvent args)
    {
        var message = args.Message;

        message = _replacement.ApplyReplacements(message, "basic_archaic_accent");

        args.Message = message;
    }
}
