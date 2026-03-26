using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Content.Shared.Speech;

namespace Content.Server.Speech.EntitySystems;

public sealed class StiltedSpeechSystem : EntitySystem
{
    // @formatter:off
    private static readonly Regex RegexStartOfWord = new(@"(?:^|\s)([a-z])");
    // @formatter:on

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StiltedSpeechComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(Entity<StiltedSpeechComponent> entity, ref AccentGetEvent args)
    {
        var message = args.Message;

        // Capitalize the first letter of every word
        message = RegexStartOfWord.Replace(message, match => match.Value.ToUpper());

        args.Message = message;
    }
}
