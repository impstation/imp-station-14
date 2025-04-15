using Content.Server._Impstation.Access.Components;
using System.Linq;
using Content.Server.Popups;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using static Content.Shared.Access.Components.AccessOverriderComponent;

namespace Content.Server._Impstation.Access.Systems;

public sealed class KeyringSystem : EntitySystem
{
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KeyringComponent, AfterInteractEvent>(AfterInteractOn);
        SubscribeLocalEvent<KeyringComponent, DoAfterEvent>(OnDoAfter);
    }

    private void AfterInteractOn(EntityUid uid, KeyringComponent component, AfterInteractEvent args)
    {
        if (args.Target == null || !TryComp(args.Target, out KeyringComponent? keyring))
            return;

        if (!_interactionSystem.InRangeUnobstructed(args.User, (EntityUid) args.Target))
            return;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, component.DoAfter, new KeyringDoAfterEvent(), uid, target: args.Target, used: uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        _doAfterSystem.TryStartDoAfter(doAfterEventArgs);
    }

}
