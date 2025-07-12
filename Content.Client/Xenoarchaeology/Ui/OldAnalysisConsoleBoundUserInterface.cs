//#IMP This file hates being in _Impstation and I don't know how to fix that
using Content.Shared.Xenoarchaeology.Equipment;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.Xenoarchaeology.Ui;

[UsedImplicitly]
public sealed class OldAnalysisConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private OldAnalysisConsoleMenu? _consoleMenu;

    public OldAnalysisConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _consoleMenu = this.CreateWindow<OldAnalysisConsoleMenu>();

        _consoleMenu.OnOldServerSelectionButtonPressed += () =>
        {
            SendMessage(new OldAnalysisConsoleServerSelectionMessage());
        };
        _consoleMenu.OnScanButtonPressed += () =>
        {
            SendMessage(new AnalysisConsoleScanButtonPressedMessage());
        };
        _consoleMenu.OnPrintButtonPressed += () =>
        {
            SendMessage(new AnalysisConsolePrintButtonPressedMessage());
        };
        _consoleMenu.OnOldExtractButtonPressed += () =>
        {
            SendMessage(new OldAnalysisConsoleExtractButtonPressedMessage());
        };
        _consoleMenu.OnUpBiasButtonPressed += () =>
        {
            SendMessage(new AnalysisConsoleBiasButtonPressedMessage(false));
        };
        _consoleMenu.OnDownBiasButtonPressed += () =>
        {
            SendMessage(new AnalysisConsoleBiasButtonPressedMessage(true));
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        switch (state)
        {
            case OldAnalysisConsoleUpdateState msg:
                _consoleMenu?.SetButtonsDisabled(msg);
                _consoleMenu?.UpdateInformationDisplay(msg);
                _consoleMenu?.UpdateProgressBar(msg);
                break;
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _consoleMenu?.Dispose();
    }
}