using Content.Shared._Impstation.Fishing.Components;
using Content.Shared._Impstation.Fishing.Systems;
using Content.Shared.EntityTable;
using Content.Shared.Interaction.Events;
using Content.Shared.Physics;
using Robust.Shared.Random;

namespace Content.Server._Impstation.Fishing;

public sealed class FishingRodSystem : SharedFishingRodSystem
{
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FishingRodComponent, UseInHandEvent>(OnRodUse);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<FishingRodComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.TargetPool == null || Timing.CurTime < comp.NextPullTime)
                continue;

            if (comp.HasBite)
            {
                if (Timing.CurTime >= comp.NextPullTime + comp.PullWindow)
                {
                    Popup.PopupEntity(Loc.GetString("fishing-rod-catch-lost"), uid);
                    comp.NextPullTime = Timing.CurTime + Random.Next(comp.MinPullTime, comp.MaxPullTime);
                    comp.HasBite = false;
                }

                continue;
            }

            if (!Random.Prob(comp.PullChance))
            {
                comp.NextPullTime = Timing.CurTime + Random.Next(comp.MinPullTime, comp.MaxPullTime);
                continue;
            }

            Popup.PopupEntity(Loc.GetString("fishing-rod-catch-bite"), uid);
            comp.HasBite = true;
        }
    }

    private void OnRodUse(Entity<FishingRodComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.TargetPool is not { } pool ||
            !TryComp<FishingPoolComponent>(pool, out var poolComp))
            return;

        var fisherCoords = Transform(ent).Coordinates;
        var poolCoords = Transform(pool).Coordinates;

        if (ent.Comp.HasBite)
        {
            foreach (var spawn in _entityTable.GetSpawns(poolComp.Table))
            {
                var spawned = Spawn(spawn, poolCoords);
                var spawnedPos = _transform.ToMapCoordinates(poolCoords);
                var fisherPos = _transform.ToMapCoordinates(fisherCoords);
                var throwDir = (fisherPos.Position - spawnedPos.Position) * ent.Comp.PullStrength;
                Throwing.TryThrow(spawned, throwDir, compensateFriction: true);
            }
        }

        RemCompDeferred<JointVisualsComponent>(ent);
        ent.Comp.TargetPool = null;
        ent.Comp.HasBite = false;
        Dirty(ent, ent.Comp);

        if (ent.Comp.Bobber is { } bobber)
            ItemSlots.TryInsert(ent.Owner, ent.Comp.BobberSlotId, bobber, ent.Comp.Holder);
    }
}
