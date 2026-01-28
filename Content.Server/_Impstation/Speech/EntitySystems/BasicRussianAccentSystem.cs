using System.Text;
using Content.Server._Impstation.Speech.Components;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Speech;

namespace Content.Server._Impstation.Speech.EntitySystems;

public sealed class BasicRussianAccentSystem : EntitySystem
{
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<BasicRussianAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(Entity<BasicRussianAccentComponent> entity, ref AccentGetEvent args)
    {
        args.Message = _replacement.ApplyReplacements(args.Message, "basicrussian");
    }
}
