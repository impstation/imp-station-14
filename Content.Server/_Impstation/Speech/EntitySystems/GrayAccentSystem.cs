using System.Text.RegularExpressions;
using Content.Server._Impstation.Speech.Components;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Speech;

namespace Content.Server._Impstation.Speech.EntitySystems;

public sealed class GrayAccentComponentAccentSystem : EntitySystem
{
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    private static readonly Regex RegexPuUpperLeft = new(@"(?<=\b[A-Z]+.)\b[Pp]u\b");
    private static readonly Regex RegexPuUpperRight = new(@"\b[Pp]u\b(?=.[A-Z]+\b)");
    private static readonly Regex RegexCatchIapostrophe = new(@"\b[Ii](?='+)\b");
    private static readonly Regex RegexThuiLower = new(@"(?<!^)(?<!\.\s+)\b[Tt]hui\b");
    private static readonly Regex RegexThuiUpperLeft = new(@"(?<=\b[A-Z]+.)\b[Tt]hui\b");
    private static readonly Regex RegexThuiUpperRight = new(@"\b[Tt]hui\b(?=.[A-Z]+\b)");
    private static readonly Regex RegexAmContraction = new(@"(?<=[a-z])'[Mm]\b");
    private static readonly Regex RegexAmContractionUpper = new(@"(?<=[A-Z])'[Mm]\b");
    private static readonly Regex RegexAreContraction = new(@"(?<=[a-z])'[Rr][Ee]\b");
    private static readonly Regex RegexAreContractionUpper = new(@"(?<=[A-Z])'[Rr][Ee]\b");

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GrayAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(Entity<GrayAccentComponent> entity, ref AccentGetEvent args)
    {
        var message = args.Message;

        args.Message = Grayspeakify(message);
    }

    public string Grayspeakify(string input)
    {
        input = _replacement.ApplyReplacements(input, "gray_accent");

        input = RegexPuUpperLeft.Replace(input, "PU");
        input = RegexPuUpperRight.Replace(input, "PU");
        input = RegexCatchIapostrophe.Replace(input, "Thui");
        input = RegexThuiLower.Replace(input, "thui");
        input = RegexThuiUpperLeft.Replace(input, "THUI");
        input = RegexThuiUpperRight.Replace(input, "THUI");
        input = RegexAmContraction.Replace(input, "-wa");
        input = RegexAmContractionUpper.Replace(input, "-WA");
        input = RegexAreContraction.Replace(input, "zz");
        input = RegexAreContractionUpper.Replace(input, "ZZ");

        return input;
    }
}
