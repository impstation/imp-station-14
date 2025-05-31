using System.Numerics;
using Content.Shared._RMC14.Hands;

namespace Content.Shared._RMC14.Sprite;

public abstract class SharedRMCSpriteSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;


    public DrawDepth.DrawDepth GetDrawDepth(EntityUid ent, DrawDepth.DrawDepth current = DrawDepth.DrawDepth.Mobs)
    {
        var ev = new GetDrawDepthEvent(current);
        RaiseLocalEvent(ent, ref ev);
        return ev.DrawDepth;
    }

    public virtual DrawDepth.DrawDepth UpdateDrawDepth(EntityUid sprite)
    {
        var depth = GetDrawDepth(sprite);
        _appearance.SetData(sprite, RMCSpriteDrawDepth.Key, depth);
        return depth;
    }
}
