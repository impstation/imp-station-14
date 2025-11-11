using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Content.Server.Speech.Prototypes;
using Content.Shared.Speech;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using JetBrains.Annotations;


namespace Content.Server.Speech.EntitySystems;

public sealed class AdvancedFullReplacementAccentSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ILocalizationManager _loc = default!;

    private static readonly Regex AllCaps = new Regex("\\b[A-Z0-9\\W]\\b");
    private static readonly Regex Punct = new Regex("[.?!]");

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
            if (!_proto.TryIndex<AdvancedFullReplacementAccentPrototype>(accent, out var prototype))
                return message;
            var messageWords = message.Split();
            var replacedMessage = "";
            var punct="";
            var cachedReplacements=GetCachedReplacements(prototype);

            foreach (char c in messageWords[^1])
            {
                if (Punct.IsMatch(c.ToString()))
                {
                    punct += c.ToString();
                }
            }

            foreach (var word in messageWords)
            {
                var isAllCaps = word.All(c => char.IsUpper(c));
                var replacement = _random.Pick(cachedReplacements).cached;
                var replacedWord = "";
                if (replacement.LengthMatch)
                {
                    var lengthToMatch = Math.Max(word.Length-(replacement.Prefix.Length+replacement.Suffix.Length), 1);
                    for (int i = 0; i < lengthToMatch; i++)
                    {
                        replacedWord += replacement.Word;
                    }
                    replacedWord=replacement.Prefix+replacedWord+replacement.Suffix;
                    if (isAllCaps)
                        replacedWord=replacedWord.ToUpper();
                }
                else
                {
                    replacedWord=replacement.Word;
                    if (isAllCaps)
                        replacedWord=replacedWord.ToUpper();
                }
                replacedMessage += " "+ replacedWord;
            }

            replacedMessage += punct;
            return replacedMessage;
        }

    private (CachedWord cached, float weight)[] GetCachedReplacements(AdvancedFullReplacementAccentPrototype prototype)
    {
        if (!_cachedReplacements.TryGetValue(prototype.ID, out var replacements))
        {
            replacements = GenerateCachedReplacements(prototype);
            _cachedReplacements.Add(prototype.ID, replacements);
        }

        return replacements;
    }

    private (CachedWord cached, float weight)[] GenerateCachedReplacements(AdvancedFullReplacementAccentPrototype prototype)
    {
        if (prototype.Words is not { } words)
            return [];

        return words.Select(kv =>
            {
                var (word, weight) = kv;
                CachedWord cached;
                if (word.LengthMatch)
                {
                    cached = new CachedWord(
                        word.LengthMatch,
                        _loc.GetString(word.Replacement),
                        _loc.GetString(word.Prefix!),
                        _loc.GetString(word.Suffix!));
                }
                else
                {
                    cached = new CachedWord(
                        word.LengthMatch,
                        word.Replacement);
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
