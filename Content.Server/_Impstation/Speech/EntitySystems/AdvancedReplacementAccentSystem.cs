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

public sealed class AdvancedReplacementAccentSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ILocalizationManager _loc = default!;

    private readonly Dictionary<ProtoId<AdvancedReplacementAccentPrototype>, (CachedWord cached, float weight)[]>
        _cachedReplacements = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<AdvancedReplacementAccentComponent, AccentGetEvent>(OnAccent);

        _proto.PrototypesReloaded += OnPrototypesReloaded;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _proto.PrototypesReloaded -= OnPrototypesReloaded;
    }
    private void OnAccent(EntityUid uid, AdvancedReplacementAccentComponent component, AccentGetEvent args)
    {
        args.Message = ApplyReplacements(args.Message, component.Accent);
    }
    /// <summary>
        ///     Attempts to apply a given replacement accent prototype to a message.
        /// </summary>
        [PublicAPI]
        public string ApplyReplacements(string message, string accent)
        {
            if (!_proto.TryIndex<AdvancedReplacementAccentPrototype>(accent, out var prototype))
                return message;



            return message;
        }

    private (CachedWord cached, float weight)[] GetCachedReplacements(AdvancedReplacementAccentPrototype prototype)
    {
        if (!_cachedReplacements.TryGetValue(prototype.ID, out var replacements))
        {
            replacements = GenerateCachedReplacements(prototype);
            _cachedReplacements.Add(prototype.ID, replacements);
        }

        return replacements;
    }

    private (CachedWord cached, float weight)[] GenerateCachedReplacements(AdvancedReplacementAccentPrototype prototype)
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
