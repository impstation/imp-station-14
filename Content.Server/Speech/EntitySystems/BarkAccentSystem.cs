using Content.Shared.StatusEffectNew;
using Content.Server.Speech.Components;
using Content.Shared.Speech;
using Robust.Shared.Random;
using System.Text.RegularExpressions; // imp edit

namespace Content.Server.Speech.EntitySystems
{
    public sealed class BarkAccentSystem : EntitySystem
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
            // imp start
            { "Ah", "Arf" },
            { "AH", "ARF" },
            { "oh", "roh" },
            { "Oh", "Roh" },
            { "OH", "ROH" },
            { "uh", "ruh" },
            { "Uh", "Ruh" },
            { "UH", "RUH" }
            // imp end
        };

        public override void Initialize()
        {
            base.Initialize(); // imp edit
            SubscribeLocalEvent<BarkAccentComponent, AccentGetEvent>(OnAccent);
            SubscribeLocalEvent<BarkAccentComponent, StatusEffectRelayedEvent<AccentGetEvent>>(OnAccentRelayed);
        }

        public string Accentuate(string message)
        {
            foreach (var (word, repl) in SpecialWords)
            {
                message = message.Replace(word, repl);
            }

            return message.Replace("!", _random.Pick(Barks)); //imp edit
        }

        private void OnAccent(Entity<BarkAccentComponent> entity, ref AccentGetEvent args)
        {
            args.Message = Accentuate(args.Message);

            // imp start
            args.Message = RegexLowerScoobyRs.Replace(args.Message, "r");
            args.Message = RegexUpperScoobyRs.Replace(args.Message, "R");
            // imp end
        }

        private void OnAccentRelayed(Entity<BarkAccentComponent> entity, ref StatusEffectRelayedEvent<AccentGetEvent> args)
        {
            args.Args.Message = Accentuate(args.Args.Message);
        }
    }
}
