using System.Text.RegularExpressions;
using Content.Server._Impstation.Speech.Components;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Speech;

namespace Content.Server._Impstation.Speech.EntitySystems;
// hi, this is a copy of NoContractionsAccentSystem, split to retain function of accentless for non thaven using the trait
public sealed class ThavenAccentComponentAccentSystem : EntitySystem
{
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ThavenAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(Entity<ThavenAccentComponent> entity, ref AccentGetEvent args)
    {
        args.Message = _replacement.ApplyReplacements(args.Message, "nocontractions");
    }
}
