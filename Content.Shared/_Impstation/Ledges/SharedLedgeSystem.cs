using System.Reflection.Metadata;
using Robust.Shared.Physics.Events;
using Content.Shared.Whitelist;
using Content.Shared.Climbing.Events;

namespace Content.Shared._Impstation.Ledges;

//this is kind of just turnstile system with all the access reading stuff trimmed out and the way collision is handled flipped around
//turnstile has one direction you're allowed to go through and blocks the rest
//ledge has one direction you're blocked
public abstract partial class SharedLedgeSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LedgeComponent, PreventCollideEvent>(OnPreventCollide);
        //SubscribeLocalEvent<LedgeComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<LedgeComponent, EndCollideEvent>(OnEndCollide);
        //SubscribeLocalEvent<LedgeComponent, ClimbedOnEvent>(OnClimb);
    }

    private void OnPreventCollide(Entity<LedgeComponent> ent, ref PreventCollideEvent args)
    {
        if (args.Cancelled || !args.OurFixture.Hard || !args.OtherFixture.Hard)
            return;

        //lets stuff through if it's currently going through or isn't whitelisted to interact
        if (ent.Comp.CollideExceptions.Contains(args.OtherEntity) || _entityWhitelist.IsWhitelistFail(ent.Comp.InteractWhitelist, args.OtherEntity))
        {
            args.Cancelled = true;
            return;
        }

        //lets stuff through if its not traveling in the block direction
        if (!BlockDirection(ent, args.OtherEntity))
        {
            ent.Comp.CollideExceptions.Add(args.OtherEntity);
            args.Cancelled = true;
            Dirty(ent);           
        }
        else
            Dirty(ent);
    }

    private void OnEndCollide(Entity<LedgeComponent> ent, ref EndCollideEvent args)
    {
        if (!args.OurFixture.Hard)
        {
            ent.Comp.CollideExceptions.Remove(args.OtherEntity);
            Dirty(ent);
        }
    }

    private void OnClimb(Entity<LedgeComponent> ent, ref ClimbedOnEvent args)
    {
        ent.Comp.CollideExceptions.Add(args.Climber);
    }

    protected bool BlockDirection(Entity<LedgeComponent> ent, EntityUid other)
    {
        var xform = Transform(ent);
        var otherXform = Transform(other);

        var (pos, rot) = _transform.GetWorldPositionRotation(xform);
        var otherPos = _transform.GetWorldPosition(otherXform);

        var approachAngle = (pos - otherPos).ToAngle();
        var rotateAngle = rot.ToWorldVec().ToAngle();

        var diff = Math.Abs(approachAngle - rotateAngle);
        diff %= MathHelper.TwoPi;
        if (diff > Math.PI)
            diff = MathHelper.TwoPi - diff;

        return diff > 3 * Math.PI / 4;
    }

}
