using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Content.Shared._RMC14.Xenonids.Announce;
using Content.Shared._RMC14.Xenonids;

namespace Content.Server._RMC14.Xenonids;

[AdminCommand(AdminFlags.Moderator)]
public sealed class XenoAnnounceCommand : IConsoleCommand
{
    public string Command => "xenoannounce";
    public string Description => "Announces a message to all xenos.";
    public string Help => $"Usage: {Command} message";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var xenoAnnounce = IoCManager.Resolve<IEntityManager>().System<SharedXenoAnnounceSystem>();
        if (args.Length == 0)
        {
            shell.WriteError("Not enough arguments! Need at least 1.");
            return;
        }

        var message = string.Join(" ", args);
        message = $"\n[bold][color=#7575F3][font size=24]Queen Mother Psychic Directive[/font][/color][/bold]\n\n[color=red][font size=14]{message}[/font][/color]\n\n";
        var sound = new XenoComponent().XenoSound;
        xenoAnnounce.AnnounceAll(default, message, sound);
    }
}
