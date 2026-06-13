using System.Text.RegularExpressions;
using Content.Server._Impstation.Speech.Components;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Speech;

namespace Content.Server._Impstation.Speech.EntitySystems;

public sealed class NoContractionsAccentComponentAccentSystem : EntitySystem
{
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NoContractionsAccentComponent, AccentGetEvent>(OnAccent);
    }


    private void OnAccent(Entity<NoContractionsAccentComponent> entity, ref AccentGetEvent args)
    {
        args.Message = _replacement.ApplyReplacements(args.Message, "nocontractions");
    }
}
