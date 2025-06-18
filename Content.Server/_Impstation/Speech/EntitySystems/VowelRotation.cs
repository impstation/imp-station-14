using System.Linq;
using System.Text.RegularExpressions;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed partial class VowelRotationSystem : EntitySystem
{
    private static readonly Regex RegexLowerA = new("a");
    private static readonly Regex RegexLowerE = new("e");
    private static readonly Regex RegexLowerI = new("i");
    private static readonly Regex RegexLowerO = new("o");
    private static readonly Regex RegexLowerU = new("u");
    private static readonly Regex RegexUpperA = new("A");
    private static readonly Regex RegexUpperE = new("E");
    private static readonly Regex RegexUpperI = new("I");
    private static readonly Regex RegexUpperO = new("O");
    private static readonly Regex RegexUpperU = new("U");

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VowelRotationComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(Entity<VowelRotationComponent> ent, ref AccentGetEvent args)
    {
        var message = args.Message;

        message = RegexLowerU.Replace(message, "a");
        message = RegexLowerO.Replace(message, "u");
        message = RegexLowerI.Replace(message, "o");
        message = RegexLowerE.Replace(message, "i");
        message = RegexLowerA.Replace(message, "e");

        message = RegexUpperU.Replace(message, "A");
        message = RegexUpperO.Replace(message, "U");
        message = RegexUpperI.Replace(message, "O");
        message = RegexUpperE.Replace(message, "I");
        message = RegexUpperA.Replace(message, "E");

        args.Message = message;
    }
}