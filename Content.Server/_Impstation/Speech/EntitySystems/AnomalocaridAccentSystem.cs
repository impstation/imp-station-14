using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Speech;

namespace Content.Server._Impstation.Speech.EntitySystems;

public sealed class AnomalocaridAccentSystem : EntitySystem
{
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;
    private static readonly Regex RegexLowerbubbleBL = new Regex("bl{1,3}");
    private static readonly Regex RegexUpperbubbleBL = new Regex("BL{1,3}");
    private static readonly Regex RegexCasebubbleBL = new Regex("Bl{1,3}");
    private static readonly Regex RegexCamelbubbleBL = new Regex("bL{1,3}");
    private static readonly Regex RegexLowerbubbleGL = new Regex("gl{1,3}");
    private static readonly Regex RegexUpperbubbleGL = new Regex("GL{1,3}");
    private static readonly Regex RegexCasebubbleGL = new Regex("Gl{1,3}");
    private static readonly Regex RegexCamelbubbleGL = new Regex("gL{1,3}");
    private static readonly Regex RegexLowerdoubleKK = new Regex("(?<!c)k");
    private static readonly Regex RegexUpperdoubleKK = new Regex("(?<!C)K}");
    private static readonly Regex RegexLowerrepeatkCK = new Regex("ck{1,3}");
    private static readonly Regex RegexUpperrepeatkCK = new Regex("CK{1,3}");
    private static readonly Regex RegexCaserepeatkCK = new Regex("Ck{1,3}");
    private static readonly Regex RegexCamelrepeatkCK = new Regex("cK{1,3}");

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AnomalocaridAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, AnomalocaridAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        // blblbl
        message = RegexLowerbubbleBL.Replace(message, "blblbl");
        //
        message = RegexUpperbubbleBL.Replace(message, "BLBLBL");
        //
        message = RegexCasebubbleBL.Replace(message, "Blblbl");
        //
        message = RegexCamelbubbleBL.Replace(message, "bLBLBL");
        // glglgl
        message = RegexLowerbubbleGL.Replace(message, "glglgl");
        //
        message = RegexUpperbubbleGL.Replace(message, "GLGLGL");
        //
        message = RegexCasebubbleGL.Replace(message, "Glglgl");
        //
        message = RegexCamelbubbleGL.Replace(message, "gLGLGL");
        // k -> k-k
        message = RegexLowerdoubleKK.Replace(message, "k-k");
        //
        message = RegexUpperdoubleKK.Replace(message, "K-K");
        // ck -> ck-k
        message = RegexLowerrepeatkCK.Replace(message, "ck-k");
        //
        message = RegexUpperrepeatkCK.Replace(message, "CK-K");
        //
        message = RegexCaserepeatkCK.Replace(message, "Ck-k");
        //
        message = RegexCamelrepeatkCK.Replace(message, "cK-K");

        message = _replacement.ApplyReplacements(message, "anomalocarid");

        args.Message = message;
    }
}
