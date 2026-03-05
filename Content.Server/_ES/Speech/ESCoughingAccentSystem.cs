using System.Text;
using Content.Shared.Speech;
using Content.Shared.StatusEffectNew;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._ES.Speech;

public sealed class ESCoughingAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESCoughingAccentComponent, AccentGetEvent>(OnAccent);
        SubscribeLocalEvent<ESCoughingAccentComponent, StatusEffectRelayedEvent<AccentGetEvent>>(OnAccentRelayed);
    }

    private string Accentuate(ESCoughingAccentComponent accent, string message)
    {
        var len = message.Length;
        if (len < accent.MinMessageLength)
            return message;

        var dataset = _proto.Index(accent.CoughingInterjectionDataset);
        var sb = new StringBuilder();

        var lastInterjectionIndex = -1;
        var cutOff = false;
        for(var i = 0; i < len; i++)
        {
            // todo this should probably support unicode properly at some point but its not a big deal for us atm
            var c = message[i];

            // if last was an interjection and this is a space, dont append it at all
            // e.g. "test-HRNGH- string" -> "test-HRNGH-string"
            if (char.IsWhiteSpace(c) && (i - 1) == lastInterjectionIndex)
                continue;

            sb.Append(c);

            // skip interjecting on spaces, skip if last char was interjected (also skips first char always)
            if (char.IsWhiteSpace(c) || (i - 1) == lastInterjectionIndex || !_random.Prob(accent.InterjectionChancePerCharacter))
            {
                continue;
            }

            var interjection = Loc.GetString(_random.Pick(dataset));
            if (_random.Prob(accent.InterjectionCapitalizeChance))
                interjection = interjection.ToUpper();
            sb.Append(Loc.GetString("es-coughing-accent-message-wrap", ("random", interjection)));
            lastInterjectionIndex = i;

            if (_random.Prob(accent.CutOffMessageChancePerInterjection))
            {
                cutOff = true;
                break;
            }
        }

        var finalMessage = sb.ToString();
        // fire in engi-hrngk--
        if (cutOff)
            return Loc.GetString("es-coughing-accent-message-wrap-cut-off", ("message", finalMessage));

        return finalMessage;
    }

    private void OnAccent(Entity<ESCoughingAccentComponent> entity, ref AccentGetEvent args)
    {
        args.Message = Accentuate(entity.Comp, args.Message);
    }

    private void OnAccentRelayed(Entity<ESCoughingAccentComponent> entity, ref StatusEffectRelayedEvent<AccentGetEvent> args)
    {
        args.Args.Message = Accentuate(entity.Comp, args.Args.Message);
    }
}
