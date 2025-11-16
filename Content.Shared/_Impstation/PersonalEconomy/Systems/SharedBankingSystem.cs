using System.Diagnostics.CodeAnalysis;
using Content.Shared._Impstation.PersonalEconomy.Components;
using Content.Shared._Impstation.PersonalEconomy.Events;
using Content.Shared.Administration;
using Content.Shared.Examine;
using Content.Shared.Roles;

namespace Content.Shared._Impstation.PersonalEconomy.Systems;

/// <summary>
/// The main banking system; handles all funds transfers and keeps track of bank accounts etc etc
/// </summary>
public abstract class SharedBankingSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = null!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<BankCardComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ATMComponent, RequestTransactionMessage>(OnTransactionRequested);
    }

    private void OnTransactionRequested(Entity<ATMComponent> ent, ref RequestTransactionMessage args)
    {

        TryMakeTransaction(args.SenderAccount, args.RecipientAccount, args.Amount, args.Reason);

    }

    public bool TryMakeTransaction(AccessNumber sender, TransferNumber recipient, int amount, string reason)
    {
        if (!VerifyTransaction(sender, recipient, amount))
            return false;

        MakeTransaction(sender, recipient, amount, reason);
        return true;

    }

    private bool VerifyTransaction(AccessNumber sender, TransferNumber recipient, int amount)
    {
        //return false if neither account exists
        if (!TryGetAccount(sender, out var senderAccount) ||
            !TryGetAccountFromTransferNumber(recipient, out var recipientAccount))
            return false;

        //return true if the sender has enough money
        return senderAccount.Value.Comp.Balance >= amount;
    }

    private void MakeTransaction(AccessNumber sender, TransferNumber recipient, int amount, string reason)
    {
        //this should always be true by the time this gets called but
        //could make these out variables from the verify method, maybe?
        //will matter less when I've got a proper cache in buuuuuut
        if (!TryGetAccount(sender, out var senderAccount) || !TryGetAccountFromTransferNumber(recipient, out var recipientAccount))
            return;

        //adjust balances
        senderAccount.Value.Comp.Balance -= amount;
        recipientAccount.Value.Comp.Balance += amount;

        //add transactions!
        AddTransaction(senderAccount.Value, -amount, recipient, reason);
        AddTransaction(recipientAccount.Value, amount, senderAccount.Value.Comp.TransferNumber, reason);
    }

    private void AddTransaction(Entity<BankAccountComponent> account, int amount, int from, string reason)
    {
        //limit reason length
        if (reason.Length > 64) //todo make this length limit a cvar?
            reason = reason[..64];

        var transaction = new BankTransaction(from, amount, reason);
        account.Comp.Transactions.Insert(0, transaction);
        if (account.Comp.Transactions.Count > 10) //todo make the history limit a cvar?
            account.Comp.Transactions.RemoveAt(10);

        Dirty(account);
    }

    private void OnExamined(Entity<BankCardComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("bank-card-examine-access-number", ("number", $"{ent.Comp.AccessNumber.Number:000000}")),4);
        args.PushMarkup(Loc.GetString("bank-card-examine-transfer-number", ("number", $"{ent.Comp.TransferNumber.Number:0000}")),4);

        if (!TryGetAccount(ent.Comp.AccessNumber, out var account))
            return;

        args.PushMarkup("The two below are for testing!", 3);
        args.PushMarkup(Loc.GetString("bank-card-examine-balance", ("balance", account.Value.Comp.Balance)), 2); //todo remove this
        args.PushMarkup(Loc.GetString("bank-card-examine-salary", ("salary", account.Value.Comp.Salary)), 1); //todo remove this
    }

    public bool TryGetAccountFromTransferNumber(TransferNumber transferNumber, [NotNullWhen(true)] out Entity<BankAccountComponent>? account)
    {
        account = null;

        //todo cache this
        var accountQuery = EntityQueryEnumerator<BankAccountComponent>();
        while (accountQuery.MoveNext(out var uid, out var comp))
        {
            if (comp.TransferNumber == transferNumber)
            {
                account = (uid, comp);
                return true;
            }
        }

        return false;
    }

    public bool TryGetAccount(AccessNumber accessNumber, [NotNullWhen(true)] out Entity<BankAccountComponent>? account)
    {
        account = null;

        var accountQuery = EntityQueryEnumerator<BankAccountComponent>();
        while (accountQuery.MoveNext(out var uid, out var comp))
        {
            if (comp.AccessNumber == accessNumber)
            {
                account = (uid, comp);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Set the name for an account
    /// </summary>
    /// <param name="accessNumber"></param>
    /// <param name="name"></param>
    public virtual void SetAccountName(AccessNumber accessNumber, string name)
    {
        if (!TryGetAccount(accessNumber, out var account))
            return;

        account.Value.Comp.Name = name;
    }

    public virtual void SetAccountSalary(AccessNumber accessNumber, int salary)
    {
        if (!TryGetAccount(accessNumber, out var account))
            return;

        account.Value.Comp.Salary = salary;
    }

    public virtual void SetAccountBalance(AccessNumber accessNumber, int balance)
    {
        if (!TryGetAccount(accessNumber, out var account))
            return;

        account.Value.Comp.Balance = balance;
    }

    /// <summary>
    /// Update the details on a bank card to reflect the details of a given account.
    /// </summary>
    /// <param name="card"></param>
    /// <param name="accessNumber"></param>
    public virtual void UpdateCardDetails(Entity<BankCardComponent> card, AccessNumber accessNumber)
    {
        if (!TryGetAccount(accessNumber, out var account))
            return;

        SetCardName(card, account.Value.Comp.Name);
        SetCardNumber(card, account.Value.Comp.AccessNumber);
    }

    /// <summary>
    /// Set the name on a card
    /// </summary>
    /// <param name="card"></param>
    /// <param name="name"></param>
    public virtual void SetCardName(Entity<BankCardComponent> card, string name)
    {
        card.Comp.Name = name;
        _metaData.SetEntityName(card, Loc.GetString(card.Comp.NamedLocId, ("name", name)));
        Dirty(card);
    }

    /// <summary>
    /// set the number on a card
    /// </summary>
    /// <param name="card"></param>
    /// <param name="accessNumber"></param>
    public virtual void SetCardNumber(Entity<BankCardComponent> card, AccessNumber accessNumber)
    {
        card.Comp.AccessNumber = accessNumber;
        Dirty(card);
    }
}
