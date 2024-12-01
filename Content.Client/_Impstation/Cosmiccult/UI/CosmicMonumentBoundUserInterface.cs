using Content.Shared._Impstation.Cosmiccult;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Impstation.Cosmiccult.UI;

[UsedImplicitly]
public sealed class CosmicMonumentBoundUserInterface : BoundUserInterface
{
    private CosmicMonumentWindow? _window;

    public CosmicMonumentBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CosmicMonumentWindow>();

        _window.OnGenerateButtonPressed += () =>
        {
            SendMessage(new CosmicMonumentGenerateButtonPressedEvent());
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not CosmicMonumentUserInterfaceState msg)
            return;
        _window?.UpdateState(msg);
    }
}

