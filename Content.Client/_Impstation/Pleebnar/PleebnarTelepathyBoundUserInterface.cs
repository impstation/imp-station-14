using Content.Shared._Impstation.Pleebnar;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._Impstation.Pleebnar;

public sealed class PleebnarTelepathyBoundUserInterface : BoundUserInterface
{

    [Dependency] private readonly IPrototypeManager _protomanager = default!;

    [ViewVariables]
    private PleebnarTelepathyWindow? _window;



    public PleebnarTelepathyBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<PleebnarTelepathyWindow>();
        _window.ReloadVisions(_protomanager);
        _window.AddVisions();

        _window.OnVisionSelect += vision => SendMessage(new PleebnarTelepathyVisionMessage(vision));

    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not PleebnarTelepathyBuiState cast || _window == null)
        {
            return;
        }

        _window.UpdateState(cast.Vision);
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _window?.Close();
    }
}
