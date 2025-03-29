using Content.Shared.Chat.TypingIndicator;

namespace Content.Shared._Impstation.MindlessClone;

public abstract class SharedMindlessCloneSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<MindlessCloneFakeTypingEvent>(OnFakeTyping);
    }
    private void ToggleFakeTypingIndicator(EntityUid entity, bool toggle)
    {
        _appearance.SetData(entity, TypingIndicatorVisuals.IsTyping, toggle);
    }

    private void OnFakeTyping(MindlessCloneFakeTypingEvent ev)
    {
        var uid = GetEntity(ev.User);

        if (!Exists(uid))
            return;

        ToggleFakeTypingIndicator(uid, ev.IsFakeTyping);
    }
}