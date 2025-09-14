using Content.Server.Resist;
using Content.Server.StationEvents.Components;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Access.Components;
using Content.Shared.Station.Components;
using Content.Shared.Storage.Components;
using Content.Shared.GameTicking.Components;

namespace Content.Server.StationEvents.Events;

public sealed class BluespaceLockerRuleRandom : StationEventSystem<BluespaceLockerRuleRandomComponent>
{
    [Dependency] private readonly BluespaceLockerSystem _bluespaceLocker = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    protected override void Started(EntityUid uid, BluespaceLockerRuleRandomComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var targets = new List<EntityUid>();
        var query = EntityQueryEnumerator<EntityStorageComponent, ResistLockerComponent>();
        while (query.MoveNext(out var storageUid, out _, out _))
        {
            targets.Add(storageUid);
        }

        RobustRandom.Shuffle(targets);
        foreach (var potentialLink in targets)
        {
            if (HasComp<AccessReaderComponent>(potentialLink) ||
                HasComp<BluespaceLockerComponent>(potentialLink) ||
                !HasComp<StationMemberComponent>(_transform.GetGrid(potentialLink)))
                continue;

            var comp = AddComp<BluespaceLockerComponent>(potentialLink);

            comp.PickLinksFromSameMap = true;
            comp.MinBluespaceLinks = 1;
            comp.BehaviorProperties.BluespaceEffectOnTeleportSource = true;
            comp.AutoLinksBidirectional = true;
            comp.AutoLinksUseProperties = true;
            comp.AutoLinkProperties.BluespaceEffectOnInit = true;
            comp.AutoLinkProperties.BluespaceEffectOnTeleportSource = true;
            _bluespaceLocker.GetTarget(potentialLink, comp, true);
            _bluespaceLocker.BluespaceEffect(potentialLink, comp, comp, true);

            // begin imp edit
            // makes bluespace lockers randomize their destinations with each use.
            comp.BehaviorProperties.ClearLinksDebluespaces = true;
            comp.BehaviorProperties.TransportEntities = true;
            comp.BehaviorProperties.ClearLinksEvery = 2;
            comp.AutoLinkProperties.DestroyAfterUses = 2;
            comp.AutoLinkProperties.DestroyType = BluespaceLockerDestroyType.DeleteComponent;
            comp.UsesSinceLinkClear = -1;
            comp.AutoLinkProperties.InvalidateOneWayLinks = true;
            comp.AutoLinkProperties.TransportEntities = false;
            // end imp edit

            Sawmill.Info($"Converted {ToPrettyString(potentialLink)} to bluespace locker");

            return;
        }
    }
}
