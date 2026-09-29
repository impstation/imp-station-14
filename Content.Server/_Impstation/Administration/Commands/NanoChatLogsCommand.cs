using Content.Server._Impstation.NanoChat;
using Content.Server.Administration;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._Impstation.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class NanoChatLogsCommand : LocalizedCommands
{
    [Dependency] private readonly EuiManager _eui = default!;

    public override string Command => "nanochatlogs";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } user)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        var ui = new AdminNanoChatLogsEui();
        _eui.OpenEui(ui, user);
    }
}
