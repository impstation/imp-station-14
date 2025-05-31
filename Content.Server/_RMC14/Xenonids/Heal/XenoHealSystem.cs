using Content.Server.Chat.Systems;
using Content.Server.Ghost;
using Content.Server.Mind;
using Content.Shared._RMC14.Xenonids.Heal;
using Content.Shared.Mind;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Xenonids.Heal;

public sealed partial class XenoHealSystem : SharedXenoHealSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly MindSystem _mind = default!;

}
