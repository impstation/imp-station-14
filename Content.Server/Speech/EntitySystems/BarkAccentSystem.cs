using Content.Server.Speech.Components;
using Content.Shared.Speech.EntitySystems;
using Robust.Shared.Random;
using System.Text.RegularExpressions; // imp edit

namespace Content.Server.Speech.EntitySystems;

public sealed class BarkAccentSystem : RelayAccentSystem<BarkAccentComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;

    // imp start
    // Regex pattern matches the first letters of words as long as they're consonants.
    private static readonly Regex RegexLowerScoobyRs = new Regex(@"\b[bcdfghjklnpqsvw]");
    private static readonly Regex RegexUpperScoobyRs = new Regex(@"\b[BCDFGHJKLNPQSVW]");
    // imp end

    private static readonly IReadOnlyList<string> Barks = new List<string>{
            " Woof!", " WOOF", " wof-wof"
        }.AsReadOnly();

    private static readonly IReadOnlyDictionary<string, string> SpecialWords = new Dictionary<string, string>()
        {
            { "ah", "arf" },
            { "Ah", "Arf" },
            // { "oh", "oof" }, // imp remove
            // { "Oh", "Oof" }, // imp remove
            // imp start
            { "AH", "ARF" },
            { "oh", "roh" },
            { "Oh", "Roh" },
            { "OH", "ROH" },
            { "uh", "ruh" },
            { "Uh", "Ruh" },
            { "UH", "RUH" }
            // imp end
        };

    protected override string AccentuateInternal(EntityUid uid, BarkAccentComponent comp, string message)
    {
        foreach (var (word, repl) in SpecialWords)
        {
            message = message.Replace(word, repl);
        }

        message = message.Replace("!", _random.Pick(Barks)); // imp edit, was return
        //  .Replace("l", "r").Replace("L", "R"); // imp remove. did they not think this through?????
        // imp add start
        message = RegexLowerScoobyRs.Replace(message, "r");
        return RegexUpperScoobyRs.Replace(message, "R");
        // imp end
    }
}
