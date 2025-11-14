using System.Numerics;
using Content.Shared._Impstation.PersonalEconomy.Components;
using Robust.Client.UserInterface;

namespace Content.Client._Impstation.PersonalEconomy.UI;

public sealed class ATMBoundUserInterface : BoundUserInterface
{

    private readonly ClientBankingSystem _banking;

    private ATMMenu? _menu;
    private TransactionWindow? _transactionWindow;
    private BankAccount? _account;

    public ATMBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _banking = EntMan.System<ClientBankingSystem>();
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<ATMMenu>();
        _menu.CreateInvalidInfoBox();
        _menu.OnClose += ClearMenu;

        _menu.OnNumberEntered += s =>
        {
            if (!int.TryParse(s, out var i) || !_banking.TryGetAccount(i, out var account))
            {
                ClearMenu();
                return;
            }

            _menu.CreateInfoBoxForAccount(account);
            _account = account;
        };

        _menu.OnClearButtonPressed += ClearMenu;

        _transactionWindow = new TransactionWindow();

        _menu.OnTransactionButtonPressed += () =>
        {
            if (_transactionWindow.IsOpen)
            {
                _transactionWindow.Close();
                return;
            }

            _transactionWindow.TransferNumberBox.Clear();
            _transactionWindow.TransferAmountBox.Clear();
            _transactionWindow.TransferReasonBox.Clear();

            _transactionWindow.Open(_menu.Position + new Vector2(_menu.Width, 0)); //todo make this part of the window and not a separate thing that just gets plonked next to it
        };

        _transactionWindow.TransactionConfirmAttempt += () =>
        {
            //todo make this a method yada yada
            if (!VerifyTransaction(_transactionWindow.TransferNumberBox.Text, _transactionWindow.TransferAmount))
            {
                _transactionWindow.ReallyConfirmButton.Disabled = true;
                return;
            }

            _transactionWindow.ReallyConfirmButton.Disabled = false;
        };

        _transactionWindow.TransactionConfirmed += () =>
        {
            //re-verify for safety
            if (!VerifyTransaction(_transactionWindow.TransferNumberBox.Text, _transactionWindow.TransferAmount))
            {
                _transactionWindow.ReallyConfirmButton.Disabled = true;
                return;
            }

            _transactionWindow.ReallyConfirmButton.Disabled = false;

            //todo do this
            /*
            SendPredictedMessage();
            */
        };
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _menu?.Dispose();
        _transactionWindow?.Dispose();
    }

    private void ClearMenu()
    {
        _menu?.CreateInvalidInfoBox();
        _account = null;
        _transactionWindow?.Close();
    }

    //todo move this into banking system
    private bool VerifyTransaction(string recipient, int amount)
    {
        var verified = true;
        //does the recipient exist?
        if (recipient.Length != 4 || //is it a valid transfer number at all
            !int.TryParse(recipient, out var number) ||
            !_banking.TryGetAccountFromTransferNumber(number, out _) //does the recipient exist?
           )
        {
            _transactionWindow!.TransactionRecipientDoesNotExistLabel.Visible = true;
            verified = false;
        }

        //do we have enough money to make the transfer?
        if (_account!.Balance < amount)
        {
            _transactionWindow!.TransactionNotEnoughFundsLabel.Visible = true;
            verified = false;
        }

        return verified;
    }
}
