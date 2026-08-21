using System.Text;
using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Content.Shared.Speech;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed class GermanAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    private static readonly Regex RegexTh = new(@"(?<=\s|^)th", RegexOptions.IgnoreCase);
    private static readonly Regex RegexThe = new(@"(?<=\s|^)the(?=\s|$)", RegexOptions.IgnoreCase);

    public override void Initialize()
    {
        SubscribeLocalEvent<GermanAccentComponent, AccentGetEvent>(OnAccent);
    }

    public string Accentuate(string message)
    {
        var msg = message;

        // rarely, "the" should become "das" instead of "ze"
        // TODO: The ReplacementAccentSystem should have random replacements this built-in.
        foreach (Match match in RegexThe.Matches(msg))
        {
            if (_random.Prob(0.1f)) //imp edit from 0.3 to 0.1, every article gets 10% chance so it isnt to common
            {
                // just shift T, H and E over to D, A and S to preserve capitalization
                msg = msg.Substring(0, match.Index) +
                      (char)(msg[match.Index] - 16) +
                      (char)(msg[match.Index + 1] - 7) +
                      (char)(msg[match.Index + 2] + 14) +
                      msg.Substring(match.Index + 3);
            }
            else //imp start
                if (_random.Prob(0.1f)) //whole bit here is imp for other articles, keeping the probality
                {
                    // shift T and H over to D and I
                    msg = msg.Substring(0, match.Index) +
                      (char)(msg[match.Index] - 16) +
                      (char)(msg[match.Index + 1] + 1) +
                      (char)(msg[match.Index + 2] + 0) +
                      msg.Substring(match.Index + 3);
                }
                else
                    if (_random.Prob(0.1f)) //whole bit here is imp for other articles 2
                    {
                        // shift T, H and E over to D, E and R to preserve capitalization
                        msg = msg.Substring(0, match.Index) +
                          (char)(msg[match.Index] - 16) +
                          (char)(msg[match.Index + 1] - 3) +
                          (char)(msg[match.Index + 2] + 13) +
                          msg.Substring(match.Index + 3);
                    } //imp end
        }

        // now, apply word replacements
        msg = _replacement.ApplyReplacements(msg, "german");

        // replace th with zh (for zhis, zhat, etc. the => ze is handled by replacements already)
        var msgBuilder = new StringBuilder(msg);
        foreach (Match match in RegexTh.Matches(msg))
        {
            // just shift the T over to a Z to preserve capitalization
            msgBuilder[match.Index] = (char)(msgBuilder[match.Index] + 6);
        }

        // Random Umlaut Time! (The joke outweighs the emotional damage this inflicts on actual Germans)
        var umlautCooldown = 0;
        for (var i = 0; i < msgBuilder.Length; i++)
        {
            if (umlautCooldown == 0)
            {
                if (_random.Prob(0.1f)) // 10% of all eligible vowels become umlauts)
                {
                    msgBuilder[i] = msgBuilder[i] switch
                    {
                        'A' => 'Ä',
                        'a' => 'ä',
                        'O' => 'Ö',
                        'o' => 'ö',
                        'U' => 'Ü',
                        'u' => 'ü',
                        _ => msgBuilder[i]
                    };
                    umlautCooldown = 4;
                }
            }
            else
            {
                umlautCooldown--;
            }
        }

        return msgBuilder.ToString();
    }

    private void OnAccent(Entity<GermanAccentComponent> ent, ref AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message);
    }
}
