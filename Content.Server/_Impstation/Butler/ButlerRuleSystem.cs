using Content.Server.Antag;
using Content.Server.Objectives.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.Rules.Components;

public sealed class ButlerRuleSystem : GameRuleSystem<ButlerRuleComponent>
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ButlerRuleComponent, AntagSelectEntityEvent>(OnAntagSelectEntity);
        SubscribeLocalEvent<ButlerRuleComponent, AfterAntagEntitySelectedEvent>(AfterAntagEntitySelected);
    }

    protected override void Started(EntityUid uid, ButlerRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        // check if we got enough potential cloning targets, otherwise cancel the gamerule so that the ghost role does not show up
        var allHumans = _mind.GetAliveHumans();

        if (allHumans.Count == 0)
        {
            Log.Info("Could not find any alive players to create a butler for! Ending gamerule.");
            ForceEndSelf(uid, gameRule);
        }
    }
    private void OnAntagSelectEntity(Entity<ButlerRuleComponent> ent, ref AntagSelectEntityEvent args)
    {
        var butler = ent.Comp.Butler;
        var target = ent.Comp.Target;
        if (args.Session?.AttachedEntity is not { } spawner)
            return;
        // get possible targets
        var allAliveHumanoids = _mind.GetAliveHumans();

        // we already checked when starting the gamerule, but someone might have died since then.
        if (allAliveHumanoids.Count == 0)
        {
            Log.Warning("Could not find any alive players to create a butler for!");
            return;
        }

        // pick a random player
        var randomHumanoidMind = _random.Pick(allAliveHumanoids);
        target = randomHumanoidMind.Comp.OwnedEntity;

        if (butler == null)
        {
            Log.Error($"Unable to make a butler of entity {ToPrettyString(butler)}");
            return;
        }

        var targetComp = EnsureComp<TargetOverrideComponent>(butler.Value);
        targetComp.Target = target; // set the kill target
    }
    private void AfterAntagEntitySelected(Entity<ButlerRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        var comp = ent.Comp;
        if (comp.Target is not { } target)
        {
            Log.Error($"Unable to find the target for the butler.");
            return;
        }
        var coords = _transform.GetMapCoordinates(target);
        _popup.PopupEntity(Loc.GetString("butler-spawn"), target, target);
        // give the target the remote
        var remote = Spawn(comp.Signaller, coords);

        // try to insert it into their bag
        if (_inventory.TryGetSlotEntity(target, "back", out var backpack))
        {
            _storage.Insert(backpack.Value, remote, out _);
        }
        else
        {
            // no bag somehow, at least pick it up
            _hands.TryPickup(target, remote);
        }
    }
}
