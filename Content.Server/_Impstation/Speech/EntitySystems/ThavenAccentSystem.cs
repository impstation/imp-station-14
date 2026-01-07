using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Content.Shared.Speech;

namespace Content.Server.Speech.EntitySystems;
// hi, this is a copy of NoContractionsAccentSystem, split to retain function of accentless for non thaven using the trait
public sealed class ThavenAccentComponentAccentSystem : EntitySystem
{
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;
    private static readonly Regex RegexLowerS = new("s+");
    private static readonly Regex RegexUpperS = new("S+");
    private static readonly Regex RegexInternalX = new(@"(\w)x");
    private static readonly Regex RegexLowerEndX = new(@"\bx([\-|r|R]|\b)");
    private static readonly Regex RegexUpperEndX = new(@"\bX([\-|r|R]|\b)");

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ThavenAccentComponent, AccentGetEvent>(OnAccent);
    }

    public string Accentuate(string message)
    {
        var accentedMessage = _replacement.ApplyReplacements(message, "nocontractions");
        return accentedMessage.ToString();
    }

    private void OnAccent(EntityUid uid, ThavenAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message);
    }
}
