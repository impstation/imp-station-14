using Content.Server.Speech.Components;
using Content.Shared.Speech;

namespace Content.Server.Speech.EntitySystems;

public sealed class ArchaicAccentSystem : EntitySystem
{
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ArchaicAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(Entity<ArchaicAccentComponent> entity, ref AccentGetEvent args)
    {
        var message = args.Message;

        message = _replacement.ApplyReplacements(message, "archaic_accent");

        args.Message = message;
    }
}
