using System.Linq;
using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Content.Server.Speech.Prototypes;
using Content.Shared.Speech;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Random.Helpers;
using JetBrains.Annotations;

namespace Content.Server.Speech.EntitySystems;

/// <remarks>
/// This is largely taken from ReplacementAccentSystem. Just altered to fit this system. the function of onAccent is different though.
/// </remarks>
public sealed class AdvancedFullReplacementAccentSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ILocalizationManager _loc = default!;

    private static readonly Regex AllCaps = new Regex("^\\P{Ll}*$");
    private static readonly Regex Punctuation = new Regex("[.?!]");

    private readonly Dictionary<ProtoId<AdvancedFullReplacementAccentPrototype>, (CachedWord cached, float weight)[]>
        _cachedReplacements = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<AdvancedFullReplacementAccentComponent, AccentGetEvent>(OnAccent);

        _proto.PrototypesReloaded += OnPrototypesReloaded;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _proto.PrototypesReloaded -= OnPrototypesReloaded;
    }
    private void OnAccent(EntityUid uid, AdvancedFullReplacementAccentComponent component, AccentGetEvent args)
    {
        args.Message = ApplyReplacements(args.Message, component.Accent);
    }

    /// <summary>
    ///     Attempts to apply a given replacement accent prototype to a message.
    /// </summary>
    [PublicAPI]
    public string ApplyReplacements(string message, string accent)
    {
        //check if the prototype actually exists
        if (!_proto.TryIndex<AdvancedFullReplacementAccentPrototype>(accent, out var prototype))
            return "";

        var messageWords = message.Split();
        var replacedMessage = "";
        var punct="";
        var cachedReplacements=GetCachedReplacements(prototype);
        if (cachedReplacements.Count <= 0)
            return "";

        //get the punctuation from the end of the message because its what matters for formatting.
        foreach (Match c in Punctuation.Matches(messageWords[^1]))
        {
            punct += c.Value;
        }

        foreach (var word in messageWords)// iterate through the words
        {
            var isAllCaps = AllCaps.IsMatch(word);
            var replacement = _random.Pick(cachedReplacements);
            var replacedWord = "";

            //check if we match the length
            if (replacement.LengthMatch)
            {
                //get the length that we need to repeat. minus the chars in the suffix or prefix, is at minimum 1.
                var lengthToMatch = Math.Max(word.Length-(replacement.Prefix.Length+replacement.Suffix.Length), 1);

                //repeat the replacement until we get to the length or higher
                while (replacedWord.Length < lengthToMatch)
                {
                    replacedWord += replacement.Word;
                }
                replacedWord=replacement.Prefix+replacedWord+replacement.Suffix;

            }
            else
            {
                replacedWord=replacement.Word;
            }
            //if its just upper case I we don't wanna make it uppercase.

            if (isAllCaps&&!word.Equals("I"))
                replacedWord=replacedWord.ToUpper();

            replacedMessage += " "+ replacedWord;
        }

        replacedMessage = replacedMessage.TrimStart();
        if (replacedMessage.Length>1)
            replacedMessage = replacedMessage[0].ToString().ToUpper()+replacedMessage.Substring(1);
        replacedMessage += punct;
        return replacedMessage;
    }

    private Dictionary<CachedWord, float> GetCachedReplacements(AdvancedFullReplacementAccentPrototype prototype)
    {
        if (!_cachedReplacements.TryGetValue(prototype.ID, out var replacements))
        {
            replacements = GenerateCachedReplacements(prototype);
            _cachedReplacements.Add(prototype.ID, replacements);
        }

        return replacements.ToDictionary();
    }

    private (CachedWord cached, float weight)[] GenerateCachedReplacements(AdvancedFullReplacementAccentPrototype prototype)
    {
        if (prototype.Words is not { } words)
            return [];


        return words.Select(kv =>
            {
                var (wordID, weight) = kv;
                if (!_proto.TryIndex(wordID, out var word))
                    return default;
                CachedWord cached;
                if (word.LengthMatch)
                {
                    cached = new CachedWord(
                        word.LengthMatch,
                        _loc.GetString(word.Replacement),
                        _loc.GetString(word.Prefix ?? ""),
                        _loc.GetString(word.Suffix ?? ""));
                }
                else
                {
                    cached = new CachedWord(
                        word.LengthMatch,
                        _loc.GetString(word.Replacement));
                }

                return (cached, weight);

            })
            .ToArray();
    }
    private void OnPrototypesReloaded(PrototypesReloadedEventArgs obj)
    {
        _cachedReplacements.Clear();
    }


}
