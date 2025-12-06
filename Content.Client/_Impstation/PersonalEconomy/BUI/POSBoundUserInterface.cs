using Content.Client._Impstation.PersonalEconomy.UI.POS;
using Content.Shared._Impstation.PersonalEconomy.Components;
using Content.Shared._Impstation.PersonalEconomy.Events;
using Content.Shared.Xenoarchaeology.Artifact.XAE;
using Robust.Client.UserInterface;

namespace Content.Client._Impstation.PersonalEconomy.BUI;

public sealed class POSBoundUserInterface : BoundUserInterface
{

    private ClientBankingSystem _banking;
    private POSMenu? _menu;

    public POSBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _banking = EntMan.System<ClientBankingSystem>();
    }

    //why does doing any UI work make me feel like I've been kicked in the head by a horse
    //abandon hope, all ye who enter here

    //ok I'm writing out a flowchart for this since I can't keep it all in my head
    //UI opened
        //if we don't have a recipient account or the user is the recipient, create a setup box
            //if the user isn't holding a card, tell them to present one or enter a number
            //if they are, give them the big ol' setup button
                //setup button pressed, hide the button & label, show the proper setup dialogue - this can just go in the setup box
                    //confirm button pressed, set everything up on the comp, keep the window open?
        //if we have a recipient, create a payment box
            //if the user isn't holding a card, tell them to present one or enter a number
            //if they are, give them the payment dialogue

    protected override void Open()
    {
        base.Open();

        var comp = EntMan.GetComponent<PosSystemComponent>(Owner);
        var recipientAccount = comp.RecipientAccount;
        var hasRecipient = recipientAccount != 0;

        _menu = this.CreateWindow<POSMenu>();

        CreateRelevantBox(hasRecipient);

        _menu.OnNumberEntered += s =>
        {
            //these don't actually need to be here but I'm too lazy to fix it rn
            var localComp = EntMan.GetComponent<PosSystemComponent>(Owner);
            var localRecipientAccount = localComp.RecipientAccount;
            var localHasRecipient = localRecipientAccount != 0;

            //if an invalid number was entered
            if (!int.TryParse(s, out var userAccount) || !_banking.TryGetAccount(userAccount, out var account))
            {
                CreateRelevantBox(localHasRecipient);
                return;
            }

            //else, if the we have a recipient, open the "payment" menu unless the recipient is the one opening the menu
            var isRecipient = localRecipientAccount == account.Value.Comp.TransferNumber;
            if (localHasRecipient && !isRecipient)
            {
                //we have a recipient and a valid number, set up the payment menu
                var box = _menu.CreatePaymentBox();
                _banking.TryGetAccountFromTransferNumber(localComp.RecipientAccount, out var recipient);
                //just assume that the bank account will not be null at this point. god help me for when I get around to deleting accounts (:
                //todo make this whole UI account for the fact that these accounts could all get deleted at some point
                //todo also do that for the other one
                box.FillOutDetails(recipient!.Value.Comp.Name, recipient.Value.Comp.TransferNumber, localComp.Amount, localComp.Reason);

                box.TransactionCancelled += () => _menu.UIKeypad.ClearButtonPressed(); //evil jank go!

                box.TransactionConfirmed += () =>
                {
                    //ok, so, we want to do a transaction finally
                    //we need to verify we can do it, then we either send off the "transaction confirmed" message or the "transaction declined" message
                    if (!VerifyTransaction(localComp.RecipientAccount, userAccount, localComp.Amount))
                    {
                        box.NoFundsLabel.Visible = true;
                        SendPredictedMessage(new PoSTransactionFailedMessage(userAccount));
                    }
                    else
                    {
                        box.NoFundsLabel.Visible = false;
                        SendPredictedMessage(new PoSTransactionSuccededMessage(userAccount));
                        Close();
                    }
                };
            }
            else
            {
                var box = _menu.CreateSetupBox();
                if (localHasRecipient)
                {
                    //if we have a recipient, fill out all the details from the component
                    box.FillOutDetails(localComp.RecipientAccount, localComp.Amount, localComp.Reason);
                }
                else
                {
                    //else, just fill out the transfer number for the current user
                    box.TransferNoEntryBox.Text = $"{account.Value.Comp.TransferNumber.Number:0000}";
                }

                box.OnSetupCleared += () =>
                {
                    SendPredictedMessage(new UpdatePoSSettingsMessage(0, 0, ""));
                    _menu.CreateInvalidSetupBox();
                };

                box.OnSetupConfirmed += () =>
                {
                    var valid = true;
                    //if the recipient doesn't exist, say what's going wrong and mark this as invalid
                    if (!VerifyRecipient(box.TransferNoEntryBox.Text, out var number))
                    {
                        box.InvalidRecipientLabel.Visible = true;
                        valid = false;
                    }

                    //if the transfer amount is 0, say what's going on and mark this as invalid
                    if (box.TransferAmount == 0)
                    {
                        box.InvalidTransferAmountLabel.Visible = true;
                        valid = false;
                    }

                    if (!valid)
                    {
                        box.SetupConfirmedLabel.Visible = false;
                        return;
                    }

                    box.InvalidRecipientLabel.Visible = false;
                    box.InvalidTransferAmountLabel.Visible = false;
                    box.SetupConfirmedLabel.Visible = true;

                    var amount = box.TransferAmount;
                    var reason = box.TransferReasonEntryBox.Text;

                    SendPredictedMessage(new UpdatePoSSettingsMessage(number, amount, reason));
                };
            }
        };

        _menu.OnClearButtonPressed += () =>
        {
            CreateRelevantBox(comp.RecipientAccount != 0);
        };
    }

    private void CreateRelevantBox(bool hasRecipient)
    {
        //if we have a recipient account set, setup a payment menu
        if (hasRecipient)
        {
            _menu!.CreateInvalidPaymentBox();
        }
        else //else, setup a setup menu
        {
           _menu!.CreateInvalidSetupBox();
        }
    }

    //todo these should probably be in a helpers file?
    private bool VerifyRecipient(string recipient, out int recipientNumber)
    {
        recipientNumber = 0;

        var rightLength = recipient.Length == 4;
        if (!rightLength)
            return false;

        var isInt = int.TryParse(recipient, out var number);
        if (!isInt)
            return false;

        var exists = _banking.TryGetAccountFromTransferNumber(number, out _);
        if (!exists)
            return false;

        recipientNumber = number;
        return true;
    }

    private bool VerifyTransaction(int recipient, int sender, int amount)
    {
        if (!_banking.TryGetAccountFromTransferNumber(recipient, out var recipientAcc) ||
            !_banking.TryGetAccount(sender, out var senderAcc) ||
            !(senderAcc.Value.Comp.Balance > amount))
            return false;

        return true;
    }
}
