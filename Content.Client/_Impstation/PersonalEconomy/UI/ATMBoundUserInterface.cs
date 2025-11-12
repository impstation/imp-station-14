using Content.Shared._Impstation.PersonalEconomy.Systems;
using Robust.Client.UserInterface;

namespace Content.Client._Impstation.PersonalEconomy.UI;

public sealed class ATMBoundUserInterface : BoundUserInterface
{

    private readonly ClientBankingSystem _banking;

    private ATMMenu? _menu;

    public ATMBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _banking = EntMan.System<ClientBankingSystem>();
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<ATMMenu>();
        _menu.CreateInvalidInfoBox();

        _menu.OnNumberEntered += s =>
        {
            if (!int.TryParse(s, out var i) || !_banking.TryGetAccount(i, out var account))
            {
                _menu.CreateInvalidInfoBox();
                return;
            }

            _menu.CreateInfoBoxForAccount(account);
        };

        _menu.OnClearButtonPressed += () =>
        {
            _menu.CreateInvalidInfoBox();
        };
    }
}
