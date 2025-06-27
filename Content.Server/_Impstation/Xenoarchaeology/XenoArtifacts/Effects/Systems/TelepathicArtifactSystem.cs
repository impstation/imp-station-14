using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

public sealed class TelepathicArtifactSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TelepathicArtifactComponent, ArtifactActivatedEvent>(OnActivate);
    }

    private void OnActivate(Entity<TelepathicArtifactComponent> ent, ref ArtifactActivatedEvent args)
    {
        // try to find victims nearby
        var victims = _lookup.GetEntitiesInRange(ent, ent.Comp.Range);
        foreach (var victimUid in victims)
        {
            if (!EntityManager.HasComponent<ActorComponent>(victimUid))
                continue;

            // roll if msg should be usual or drastic
            List<string> msgArr;
            if (_random.NextFloat() <= ent.Comp.DrasticMessageProb && ent.Comp.DrasticMessages != null)
            {
                msgArr = ent.Comp.DrasticMessages;
            }
            else
            {
                msgArr = ent.Comp.Messages;
            }

            // pick a random message
            var msgId = _random.Pick(msgArr);
            var msg = Loc.GetString(msgId);

            // show it as a popup, but only for the victim
            _popupSystem.PopupEntity(msg, victimUid, victimUid);
        }
    }
}