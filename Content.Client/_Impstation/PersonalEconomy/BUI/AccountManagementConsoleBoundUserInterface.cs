using Content.Client._Impstation.PersonalEconomy.UI.AccountManagement;
using Robust.Client.UserInterface;

namespace Content.Client._Impstation.PersonalEconomy.BUI;

public sealed class AccountManagementConsoleBoundUserInterface : BoundUserInterface
{

    private readonly ClientBankingSystem _banking;
    private AccountManagementMenu? _window;

    public AccountManagementConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _banking = EntMan.System<ClientBankingSystem>();
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<AccountManagementMenu>();
        _window.OpenCentered();
        _window.FillOutAccountButtons(EntMan);
    }
}
