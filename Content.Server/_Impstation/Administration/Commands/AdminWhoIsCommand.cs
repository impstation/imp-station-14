using System.Linq;
using System.Text;
using Content.Server.Administration;
using Content.Server.Afk;
using Content.Server.Mind;
using Content.Shared.Administration;
using Content.Shared.Atmos.Components;
using Content.Shared.Mind.Components;
using Microsoft.CodeAnalysis;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Player;

namespace Content.Server._Impstation.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class AdminWhoIsCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly ISharedPlayerManager _playerMan = default!;

    public string Command => "whois";
    public string Description => "Takes a character name and returns the attached player's username.";
    public string Help => "Usage: whois [character name]";
    private static readonly string[] First = ["?"];

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var sb = new StringBuilder();
        var valid = false;

        var users = _playerMan.Sessions.Select(p => p);
        foreach (var user in users)
        {
            if (user.AttachedEntity is not { } userEntity)
                continue;
            if (_entMan.GetComponent<MetaDataComponent>(userEntity).EntityName == args[0])
            {
                sb.Append(string.Concat(Loc.GetString("admin-who-is-valid", ("username", user.Name), ("uid", userEntity.ToString()))));
                valid = true;
            }
        }
        if (valid == true)
            shell.WriteLine(sb.ToString());
        else if (args[0] == First[0])
            shell.WriteLine(Loc.GetString("admin-who-is-help"));
        else
            shell.WriteLine(Loc.GetString("admin-who-is-invalid"));
    }

    private IEnumerable<string> GetAllCharacterNames()
    {
        var afk = IoCManager.Resolve<IAfkManager>();

        List<string> names = [];

        var users = _playerMan.Sessions.Select(p => p);
        foreach (var user in users)
        {
            if (user.AttachedEntity is not { } userEntity)
                continue;
            names.Add(_entMan.GetComponent<MetaDataComponent>(userEntity).EntityName);
        }

        return names.AsEnumerable();
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var options = First.Concat(GetAllCharacterNames());

            return CompletionResult.FromHintOptions(options, "<character name | ?>");
        }

        return CompletionResult.Empty;
    }
}
