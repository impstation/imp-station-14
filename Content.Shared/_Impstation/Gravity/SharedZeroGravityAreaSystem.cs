using System.Linq;
using Content.Shared.Item.ItemToggle.Components;

namespace Content.Shared._Impstation.Gravity;

public abstract partial class SharedZeroGravityAreaSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemToggleZeroGravityAreaComponent, ItemToggledEvent>(OnToggleGravity);
    }

    private void OnToggleGravity(EntityUid uid, ItemToggleZeroGravityAreaComponent comp, ItemToggledEvent args)
    {
        SetEnabled(uid, args.Activated);
    }

    public bool IsEnabled(EntityUid uid, ZeroGravityAreaComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return false;

        return comp.Enabled;
    }

    public abstract void SetEnabled(EntityUid uid, bool enabled, ZeroGravityAreaComponent? comp = null);
}
