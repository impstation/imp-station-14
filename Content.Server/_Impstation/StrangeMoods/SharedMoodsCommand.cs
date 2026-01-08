using Content.Server._Impstation.StrangeMoods.Eui;
using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.StrangeMoods;

[AdminCommand(AdminFlags.Fun)]
public sealed class SharedMoodsCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly EuiManager _eui = default!;
    [Dependency] private readonly StrangeMoodsSystem _strangeMoods = default!;

    public override string Command => "sharedmoods";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        var ui = new SharedMoodsEui(_strangeMoods, _prototype, _admin, _eui, player);
        _eui.OpenEui(ui, player);
        ui.Init();
    }
}
