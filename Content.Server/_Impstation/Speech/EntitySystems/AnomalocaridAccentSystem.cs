using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Speech;

namespace Content.Server._Impstation.Speech.EntitySystems;

public sealed partial class AnomalocaridAccentSystem : EntitySystem
{
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    private static readonly Dictionary<Regex, string> Regexes = new()
    {
        {new ("bl"),"blblbl"},
        {new ("Bl"),"Blblbl"},
        {new ("BL"),"BLBLBL"},
        {new ("gl"),"glglgl"},
        {new ("Gl"),"Glglgl"},
        {new ("GL"),"GLGGLL"},
        {new ("(?<!c)k"),"k-k"},
        {new ("(?<!C)K"),"K-K"},
        {new ("ck"),"ck-k"},
        {new ("CK"),"CK-K"},
        {new ("Ck"),"Ck-k"},
        {new ("cK"),"cK-K"},
    };

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AnomalocaridAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, AnomalocaridAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        foreach (var keypair in Regexes)
        {
            message = keypair.Key.Replace(message, keypair.Value);
        }

        message = _replacement.ApplyReplacements(message, "anomalocarid");

        args.Message = message;
    }
}
