using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Speech;

namespace Content.Server._MACRO.Speech.EntitySystems;

public sealed partial class AnomalocaridAccentSystem : EntitySystem
{
    [Dependency] private ReplacementAccentSystem _replacement = default!;

    private static readonly Regex BubbleRegex = new("(([bg])|([BG]))((l)|(L))");

    private static readonly Regex ClickRegex = new("(c)(k)", RegexOptions.IgnoreCase);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AnomalocaridAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, AnomalocaridAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        // "bl" -> "blblbl"
        // "Bl" -> "Blblbl"
        // "BL" -> "BLBLBL"
        // "bL" -> "blbLBL"
        // please bear with me. regex so beautiful
        message = BubbleRegex.Replace(message, m => m.Groups[1].ToString() +
            (m.Groups[2].Success ? m.Groups[4].ToString().ToLower() : m.Groups[4].ToString()) +
            (m.Groups[5].Success ? m.Groups[1].ToString().ToLower() : m.Groups[1].ToString()) +
            m.Groups[4].ToString() +
            (m.Groups[5].Success ? m.Groups[1].ToString().ToLower() : m.Groups[1].ToString().ToUpper()) +
            m.Groups[4].ToString());

        // "fuck" -> "fuck-k"
        message = ClickRegex.Replace(message, "$1$2-$2");

        message = _replacement.ApplyReplacements(message, "anomalocarid");

        args.Message = message;
    }
}
