using System.Linq;
using Content.Client.Hands.Systems;
using Content.Shared._RMC14.Hands;
using Content.Shared.Hands.Components;
using Robust.Shared.Input.Binding;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Hands;

public sealed class ClientRMCHandsSystem : RMCHandsSystem
{
    [Dependency] private readonly HandsSystem _hands = default!;
}
