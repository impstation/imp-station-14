using System.Numerics;
using Content.Client._Impstation.PersonalEconomy.UI;
using Content.Shared._Impstation.PersonalEconomy.Components;
using Content.Shared._Impstation.PersonalEconomy.Events;
using Robust.Client.UserInterface;

namespace Content.Client._Impstation.PersonalEconomy.BUI;

public sealed class ATMBoundUserInterface : BoundUserInterface
{

    private readonly ClientBankingSystem _banking;

    private ATMMenu? _atmMenu;
    private TransactionMenu? _transactionMenu;
    private Entity<BankAccountComponent>? _account;

    public ATMBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _banking = EntMan.System<ClientBankingSystem>();
    }

    protected override void Open()
    {
        base.Open();

        _atmMenu = this.CreateWindow<ATMMenu>();
        _atmMenu.CreateInvalidInfoBox();
        _atmMenu.OnClose += ClearMenu;

        _atmMenu.OnNumberEntered += s =>
        {
            if (!int.TryParse(s, out var i) || !_banking.TryGetAccount(i, out var account))
            {
                ClearMenu();
                return;
            }

            _atmMenu.CreateInfoBoxForAccount(account.Value);
            _account = account.Value;
        };

        _atmMenu.OnClearButtonPressed += ClearMenu;

        _transactionMenu = new TransactionMenu();

        _atmMenu.OnTransactionButtonPressed += () =>
        {
            if (_transactionMenu.IsOpen)
            {
                _transactionMenu.Close();
                return;
            }

            _transactionMenu.TransferNumberBox.Clear();
            _transactionMenu.TransferAmountBox.Clear();
            _transactionMenu.TransferReasonBox.Clear();
            _transactionMenu.TransactionNotEnoughFundsLabel.Visible = false;
            _transactionMenu.TransactionRecipientDoesNotExistLabel.Visible = false;

            //todo make this part of the window and not a separate thing that just gets plonked next to it
            //like the news manager console but I have no fucking idea how that does that tbh
            _transactionMenu.Open(_atmMenu.Position + new Vector2(_atmMenu.Width, 0));
        };

        _transactionMenu.TransactionConfirmAttempt += () =>
        {
            //todo make this a method yada yada
            if (!VerifyTransaction(_transactionMenu.TransferNumberBox.Text, _transactionMenu.TransferAmount))
            {
                _transactionMenu.ReallyConfirmButton.Disabled = true;
                return;
            }

            _transactionMenu.ReallyConfirmButton.Disabled = false;
        };

        _transactionMenu.TransactionConfirmed += () =>
        {
            //re-verify for safety
            if (!VerifyTransaction(_transactionMenu.TransferNumberBox.Text, _transactionMenu.TransferAmount))
            {
                _transactionMenu.ReallyConfirmButton.Disabled = true;
                return;
            }

            _transactionMenu.ReallyConfirmButton.Disabled = false;

            //not predicted because I'm handling all transaction things server-side
            //might be annoying for some things? I can always move it to shared if it gets *too* annoying to deal with
            SendPredictedMessage(
                new RequestTransactionMessage(
                    _account!.Value.Comp.AccessNumber,
                    int.Parse(_transactionMenu.TransferNumberBox.Text),
                    _transactionMenu.TransferAmount,
                    _transactionMenu.TransferReasonBox.Text)
                );

            //finally, close the menu after a theoretically successful transaction
            _transactionMenu.Close();
        };
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _atmMenu?.Dispose();
        _transactionMenu?.Dispose();
    }

    private void ClearMenu()
    {
        _atmMenu?.CreateInvalidInfoBox();
        _account = null;
        _transactionMenu?.Close();
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
            _transactionMenu!.TransactionRecipientDoesNotExistLabel.Visible = true;
            verified = false;
        }

        //do we have enough money to make the transfer?
        if (_account!.Value.Comp.Balance < amount)
        {
            _transactionMenu!.TransactionNotEnoughFundsLabel.Visible = true;
            verified = false;
        }

        return verified;
    }
}
