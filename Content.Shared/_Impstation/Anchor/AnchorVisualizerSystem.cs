using Content.Shared._Impstation.Anchor;
using Content.Shared.Construction.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Anchor;

public sealed partial class AnchorVisualizerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnchorVisualsComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<AnchorVisualsComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
    }

    private void OnStartup(EntityUid uid, AnchorVisualsComponent comp, ComponentStartup args)
    {
        _appearance.SetData(uid, AnchorVisuals.Anchored, Transform(uid).Anchored);
    }

    private void OnAnchorStateChanged(EntityUid uid, AnchorVisualsComponent comp, AnchorStateChangedEvent args)
    {
        _appearance.SetData(uid, AnchorVisuals.Anchored, args.Anchored);
    }
}

[Serializable, NetSerializable]
public enum AnchorVisuals : byte
{
    Anchored
}
