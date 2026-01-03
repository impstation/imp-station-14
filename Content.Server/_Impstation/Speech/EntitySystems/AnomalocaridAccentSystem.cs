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
        {new ("bl"),"blbl"},
        {new ("Bl"),"Blbl"},
        {new ("BL"),"BLBL"},
        {new ("gl"),"glgl"},
        {new ("Gl"),"Glgl"},
        {new ("GL"),"GLGL"},
        {new ("k"),"k-k"},
        {new ("K"),"K-K"},
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
